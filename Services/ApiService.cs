using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MobileSLI.Configuration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace MobileSLI.Services;

public sealed class ApiService
{
    /*
     * Fonctionnement actuel choisi pour le téléphone physique :
     *
     * 1. L'API tourne sur le PC sur le port 5000.
     * 2. ADB fait un tunnel entre le téléphone et le PC :
     *
     *    adb reverse tcp:5000 tcp:5000
     *
     * 3. Depuis le téléphone ou l'application mobile, l'API est accessible via :
     *
     *    http://127.0.0.1:5000
     *
     * Important :
     * - Ne pas mettre l'IP du PC ici si on utilise adb reverse.
     * - Ne pas mettre localhost par préférence, 127.0.0.1 est plus explicite.
     * - Ne pas mettre 10.0.2.2, c'est uniquement pour l'émulateur Android.
     */
    private static string DefaultBaseUrl
    {
        get
        {
            return AppConfig.ApiBaseUrl;
        }
    }

    private const string ApiBaseUrlPreferenceKey = "ApiBaseUrl";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private string _baseUrl;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

#if DEBUG
        /*
         * En Debug, on force l'URL adb reverse pour éviter qu'une ancienne valeur
         * sauvegardée dans Preferences, par exemple http://192.168.1.66:5000,
         * continue d'être utilisée par l'application.
         */
        _baseUrl = DefaultBaseUrl;
        Preferences.Set(ApiBaseUrlPreferenceKey, _baseUrl);
#else
        _baseUrl = NormalizeBaseUrl(
            Preferences.Get(ApiBaseUrlPreferenceKey, DefaultBaseUrl)
        );
#endif
    }

    public string BaseUrl => _baseUrl;

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = NormalizeBaseUrl(baseUrl);
        Preferences.Set(ApiBaseUrlPreferenceKey, _baseUrl);
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                BuildUri("/api/health"),
                cancellationToken
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiHealthResult> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        const string route = "/api/health";

        using var response = await _httpClient.GetAsync(
            BuildUri(route),
            cancellationToken
        );

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new ApiHealthResult
        {
            IsSuccess = response.IsSuccessStatusCode,
            StatusCode = (int)response.StatusCode,
            RawBody = body
        };
    }

    public async Task<IReadOnlyList<ApiLivreurDto>> GetLivreursAsync(
        CancellationToken cancellationToken = default)
    {
        const string route = "/api/livreurs";

        var livreurs = await GetAsync<List<ApiLivreurDto>>(route, cancellationToken);

        return (livreurs ?? [])
            .Where(livreur => !string.IsNullOrWhiteSpace(livreur.CodeLivreur))
            .OrderBy(livreur => TryParseInt(livreur.CodeLivreur))
            .ThenBy(livreur => livreur.NomLivreur)
            .ToList();
    }

    public Task<IReadOnlyList<ApiLivreurDto>> ChargerLivreursAsync(
        CancellationToken cancellationToken = default)
    {
        return GetLivreursAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiTourneeResumeDto>> GetTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeLivreur))
        {
            throw new ArgumentException("Le code livreur est obligatoire.", nameof(codeLivreur));
        }

        var route = BuildRoute(
            "/api/tournees/jour",
            new Dictionary<string, string?>
            {
                ["dateTournee"] = FormatDate(dateTournee),
                ["codeLivreur"] = codeLivreur.Trim()
            }
        );

        using var document = await GetJsonDocumentAsync(route, cancellationToken);

        var tournees = ExtractList<ApiTourneeResumeDto>(
            document.RootElement,
            "tournees"
        );

        return tournees
            .Where(tournee => !string.IsNullOrWhiteSpace(tournee.CodeTournee))
            .OrderBy(tournee => TryParseInt(tournee.CodeTournee))
            .ThenBy(tournee => tournee.LibelleTournee)
            .ToList();
    }

    public Task<IReadOnlyList<ApiTourneeResumeDto>> ChargerTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(dateTournee, codeLivreur, cancellationToken);
    }

    public Task<IReadOnlyList<ApiTourneeResumeDto>> GetTourneesAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(dateTournee, codeLivreur, cancellationToken);
    }

    public async Task<ApiTourneeMobileDto> GetTourneeDuJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeTournee))
        {
            throw new ArgumentException("Le code tournée est obligatoire.", nameof(codeTournee));
        }

        if (string.IsNullOrWhiteSpace(codeLivreur))
        {
            throw new ArgumentException("Le code livreur est obligatoire.", nameof(codeLivreur));
        }

        var route = BuildRoute(
            "/api/tournees/jour",
            new Dictionary<string, string?>
            {
                ["dateTournee"] = FormatDate(dateTournee),
                ["codeTournee"] = codeTournee.Trim(),
                ["codeLivreur"] = codeLivreur.Trim()
            }
        );

        var tournee = await GetAsync<ApiTourneeMobileDto>(route, cancellationToken);

        if (tournee is null)
        {
            throw new ApiServiceException(
                "La réponse de l'API est vide pour le chargement de la tournée.",
                0,
                route,
                string.Empty
            );
        }

        tournee.Livreur ??= new ApiLivreurDto();
        tournee.Chargement ??= new ApiChargementDto();
        tournee.ArticlesSaisissables ??= [];
        tournee.Lignes ??= [];

        foreach (var ligne in tournee.Lignes)
        {
            ligne.Saisie ??= new ApiSaisieLigneDto();
            ligne.Saisie.Quantites ??= [];
        }

        return tournee;
    }

    public Task<ApiTourneeMobileDto> ChargerTourneeDuJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeDuJourAsync(dateTournee, codeTournee, codeLivreur, cancellationToken);
    }

    public Task<ApiTourneeMobileDto> ChargerTourneeAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeDuJourAsync(dateTournee, codeTournee, codeLivreur, cancellationToken);
    }

    public Task<ApiTourneeMobileDto> GetTourneeAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeDuJourAsync(dateTournee, codeTournee, codeLivreur, cancellationToken);
    }

    public async Task<ApiSynchronisationResultDto> SynchroniserTourneeAsync(
        ApiSynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        const string route = "/api/synchronisations";

        using var response = await _httpClient.PostAsJsonAsync(
            BuildUri(route),
            request,
            JsonOptions,
            cancellationToken
        );

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        var result = TryDeserialize<ApiSynchronisationResultDto>(body)
            ?? new ApiSynchronisationResultDto
            {
                Statut = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR",
                Message = body
            };

        result.StatusCode = (int)response.StatusCode;
        result.RawBody = body;

        return result;
    }

    public Task<ApiSynchronisationResultDto> EnvoyerSynchronisationAsync(
        ApiSynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        return SynchroniserTourneeAsync(request, cancellationToken);
    }

    public Task<ApiSynchronisationResultDto> PostSynchronisationAsync(
        ApiSynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        return SynchroniserTourneeAsync(request, cancellationToken);
    }

    private async Task<T?> GetAsync<T>(
        string route,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildUri(route),
            cancellationToken
        );

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException(response.StatusCode, route, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ApiServiceException(
                $"La réponse JSON de l'API est invalide : {exception.Message}",
                (int)response.StatusCode,
                route,
                body
            );
        }
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(
        string route,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildUri(route),
            cancellationToken
        );

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateApiException(response.StatusCode, route, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ApiServiceException(
                "La réponse de l'API est vide.",
                (int)response.StatusCode,
                route,
                body
            );
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new ApiServiceException(
                $"La réponse JSON de l'API est invalide : {exception.Message}",
                (int)response.StatusCode,
                route,
                body
            );
        }
    }

    private Uri BuildUri(string route)
    {
        var normalizedRoute = route.StartsWith("/", StringComparison.Ordinal)
            ? route
            : "/" + route;

        return new Uri(_baseUrl + normalizedRoute, UriKind.Absolute);
    }

    private static string BuildRoute(
        string path,
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        var query = string.Join(
            "&",
            queryParameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!.Trim())}")
        );

        return string.IsNullOrWhiteSpace(query)
            ? path
            : $"{path}?{query}";
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return DefaultBaseUrl;
        }

        var normalized = baseUrl.Trim().TrimEnd('/');

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "http://" + normalized;
        }

        return normalized;
    }

    private static string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static int TryParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : int.MaxValue;
    }

    private static List<T> ExtractList<T>(JsonElement root, string preferredPropertyName)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return DeserializeList<T>(root);
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var propertyNames = new[]
        {
            preferredPropertyName,
            "items",
            "data",
            "result",
            "results",
            "value"
        };

        foreach (var propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Array)
            {
                return DeserializeList<T>(property);
            }

            if (property.ValueKind == JsonValueKind.Object)
            {
                var singleItem = property.Deserialize<T>(JsonOptions);
                return singleItem is null ? [] : [singleItem];
            }
        }

        var item = root.Deserialize<T>(JsonOptions);
        return item is null ? [] : [item];
    }

    private static List<T> DeserializeList<T>(JsonElement element)
    {
        return element.Deserialize<List<T>>(JsonOptions) ?? [];
    }

    private static T? TryDeserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static ApiServiceException CreateApiException(
        HttpStatusCode statusCode,
        string route,
        string body)
    {
        var status = (int)statusCode;

        var message = statusCode switch
        {
            HttpStatusCode.NotFound
                => $"Ressource introuvable : {route}",

            HttpStatusCode.BadRequest
                => $"Requête invalide vers l'API : {route}",

            HttpStatusCode.Conflict
                => $"Conflit métier ou technique détecté par l'API : {route}",

            HttpStatusCode.InternalServerError
                => $"Erreur technique côté API : {route}",

            _
                => $"Erreur API HTTP {status} sur {route}"
        };

        return new ApiServiceException(message, status, route, body);
    }
}

public sealed class ApiServiceException : Exception
{
    public ApiServiceException(
        string message,
        int statusCode,
        string route,
        string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        Route = route;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string Route { get; }

    public string ResponseBody { get; }
}

public sealed class ApiHealthResult
{
    public bool IsSuccess { get; set; }

    public int StatusCode { get; set; }

    public string RawBody { get; set; } = string.Empty;
}

public sealed class ApiLivreurDto
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(NomLivreur)
            ? CodeLivreur
            : $"{CodeLivreur} - {NomLivreur}";
}

public sealed class ApiTourneeResumeDto
{
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public int NombreClients { get; set; }

    public int NombrePoints { get; set; }

    public string? StatutSynchronisation { get; set; }

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} - {LibelleTournee}";
}

public sealed class ApiTourneeMobileDto
{
    public string SchemaVersion { get; set; } = "1.1";

    public DateTime DateTournee { get; set; }

    public bool DateModifiable { get; set; }

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public string? StatutSynchronisation { get; set; }

    public ApiLivreurDto? Livreur { get; set; }

    public ApiChargementDto? Chargement { get; set; }

    public List<string>? ArticlesSaisissables { get; set; } = [];

    public List<ApiTourneeLigneDto>? Lignes { get; set; } = [];

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} - {LibelleTournee}";
}

public sealed class ApiChargementDto
{
    public DateTime? DateGenerationApi { get; set; }

    public int NombrePointsEnvoyes { get; set; }
}

public sealed class ApiTourneeLigneDto
{
    public string IdLigneSource { get; set; } = string.Empty;

    public int? OrdreArret { get; set; }

    public string NumClient { get; set; } = string.Empty;

    public string NomClient { get; set; } = string.Empty;

    public string? NomAffiche { get; set; }

    public string? CodePDL { get; set; }

    public string? DescriptionPDL { get; set; }

    public string? AdresseLigne1 { get; set; }

    public string? AdresseLigne2 { get; set; }

    public string? AdresseLigne3 { get; set; }

    public string? CodePostal { get; set; }

    public string? Ville { get; set; }

    public string? Instructions { get; set; }

    public string? CommentaireFiche { get; set; }

    public string? ZoneDechargement { get; set; }

    public string? Zone { get; set; }

    public string? Precision { get; set; }

    public ApiSaisieLigneDto? Saisie { get; set; }
}

public sealed class ApiSaisieLigneDto
{
    public string StatutPassage { get; set; } = "A_FAIRE";

    public bool EstValidee { get; set; }

    public string? HeureValidation { get; set; }

    public string? CommentaireLivreur { get; set; }

    public string? PrecisionLivreur { get; set; }

    public List<ApiQuantiteSaisieDto>? Quantites { get; set; } = [];
}

public sealed class ApiQuantiteSaisieDto
{
    public string CodeArticle { get; set; } = string.Empty;

    public string LibelleArticle { get; set; } = string.Empty;

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

public sealed class ApiSynchronisationTourneeRequest
{
    public string SchemaVersion { get; set; } = "1.1";

    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();

    public DateTime DateTournee { get; set; }

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public ApiLivreurDto Livreur { get; set; } = new();

    public ApiMobileInfoDto Mobile { get; set; } = new();

    public string? CommentaireGlobal { get; set; }

    public List<ApiTourneeLigneDto> Lignes { get; set; } = [];
}

public sealed class ApiMobileInfoDto
{
    public string NomAppareil { get; set; } = DeviceInfo.Name;

    public string VersionApplication { get; set; } = AppInfo.VersionString;

    public DateTime DateEnvoi { get; set; } = DateTime.Now;

    public DateTime? DateChargement { get; set; }
}

public sealed class ApiSynchronisationResultDto
{
    public string Statut { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Message { get; set; }

    public List<string>? Errors { get; set; }

    public int StatusCode { get; set; }

    public string RawBody { get; set; } = string.Empty;

    public bool IsSuccess =>
        string.Equals(Statut, "SUCCESS", StringComparison.OrdinalIgnoreCase)
        || StatusCode is >= 200 and <= 299;

    public bool IsConflict =>
        string.Equals(Statut, "CONFLICT", StringComparison.OrdinalIgnoreCase)
        || StatusCode == 409;

    public bool IsValidationError =>
        string.Equals(Statut, "VALIDATION_ERROR", StringComparison.OrdinalIgnoreCase)
        || StatusCode == 400;
}