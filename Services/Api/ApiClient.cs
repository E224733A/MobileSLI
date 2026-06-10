using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MobileSLI.Services;

namespace MobileSLI.Services.Api;

/// <summary>
/// Client HTTP centralisé pour accéder à l'API MobileSLI.
/// Cette classe concentre la normalisation de l'URL API, la sérialisation JSON,
/// les timeouts applicatifs, les tentatives éventuelles et la traduction des erreurs réseau/API
/// en exceptions lisibles côté application mobile.
/// </summary>
public sealed class ApiClient
{
    /// <summary>
    /// Options JSON communes à tous les échanges avec l'API.
    /// Le mode Web respecte les conventions JSON usuelles et la lecture insensible à la casse
    /// protège le mobile contre de petites différences de casse entre contrats.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;

    public ApiClient(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // Timeout global de sécurité : les timeouts métier par appel restent fournis via ApiTimeouts.
        // Cette limite globale évite un blocage indéfini si le réseau ou le serveur ne répond plus.
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(180)
        };
    }

    /// <summary>
    /// URL de base réellement utilisée pour les appels API après normalisation.
    /// </summary>
    public string BaseUrl => NormalizeBaseUrl(_settingsService.ApiBaseUrl);

    /// <summary>
    /// Exécute un GET JSON avec le timeout GET par défaut et sans tentative automatique.
    /// </summary>
    public Task<T?> GetAsync<T>(
        string route,
        CancellationToken cancellationToken = default)
    {
        return GetAsync<T>(
            route,
            ApiTimeouts.DefaultGet,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }

    /// <summary>
    /// Exécute un GET JSON avec timeout et politique de tentative explicites.
    /// Lève une <see cref="ApiClientException"/> si l'API répond avec un code HTTP d'erreur
    /// ou si le JSON reçu ne respecte pas le contrat attendu par le type demandé.
    /// </summary>
    public async Task<T?> GetAsync<T>(
        string route,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        var response = await GetRawAsync(
            route,
            timeout,
            retryCount,
            retryDelay,
            cancellationToken);

        if (!response.IsSuccess)
        {
            throw CreateException(response.StatusCode, route, response.Body);
        }

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(response.Body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ApiClientException(
                $"La réponse JSON de l'API est invalide : {exception.Message}",
                response.StatusCode,
                route,
                response.Body,
                exception);
        }
    }

    /// <summary>
    /// Exécute un GET brut avec le timeout GET par défaut.
    /// À utiliser quand l'appelant doit gérer lui-même le corps de réponse.
    /// </summary>
    public Task<ApiRawResponse> GetRawAsync(
        string route,
        CancellationToken cancellationToken = default)
    {
        return GetRawAsync(
            route,
            ApiTimeouts.DefaultGet,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }

    /// <summary>
    /// Exécute un GET brut avec timeout et politique de tentative explicites.
    /// </summary>
    public Task<ApiRawResponse> GetRawAsync(
        string route,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        return SendRawAsync(
            route,
            HttpMethod.Get,
            contentFactory: null,
            timeout,
            retryCount,
            retryDelay,
            cancellationToken);
    }

    /// <summary>
    /// Envoie une requête POST JSON avec le timeout POST par défaut.
    /// </summary>
    public Task<ApiRawResponse> PostAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsJsonAsync(
            route,
            request,
            ApiTimeouts.DefaultPost,
            cancellationToken);
    }

    /// <summary>
    /// Envoie une requête POST JSON avec un timeout explicite.
    /// La sérialisation utilise les mêmes options que les autres contrats API du mobile.
    /// </summary>
    public Task<ApiRawResponse> PostAsJsonAsync<TRequest>(
        string route,
        TRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return SendRawAsync(
            route,
            HttpMethod.Post,
            () => JsonContent.Create(request, options: JsonOptions),
            timeout,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);
    }

    /// <summary>
    /// Tente de désérialiser un JSON sans lever d'exception à l'appelant.
    /// Utilisé pour exploiter un corps de réponse technique tout en conservant un flux d'erreur propre.
    /// </summary>
    public T? Deserialize<T>(string json)
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

    /// <summary>
    /// Construit une route avec query string en ignorant les paramètres vides.
    /// Les clés et valeurs sont encodées pour éviter les erreurs liées aux espaces ou caractères spéciaux.
    /// </summary>
    public string BuildRoute(
        string path,
        IReadOnlyDictionary<string, string?> queryParameters)
    {
        var query = string.Join(
            "&",
            queryParameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!.Trim())}"));

        return string.IsNullOrWhiteSpace(query)
            ? path
            : $"{path}?{query}";
    }

    /// <summary>
    /// Formate une date selon le contrat API attendu : yyyy-MM-dd en culture invariante.
    /// </summary>
    public string FormatDate(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Point d'exécution commun de tous les appels HTTP.
    /// Il applique le timeout demandé, construit la requête, lit le corps de réponse,
    /// transforme les erreurs réseau en exceptions métier et applique la politique de tentative.
    /// </summary>
    private async Task<ApiRawResponse> SendRawAsync(
        string route,
        HttpMethod method,
        Func<HttpContent?>? contentFactory,
        TimeSpan timeout,
        int retryCount,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        var attempts = Math.Max(0, retryCount) + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                using var request = new HttpRequestMessage(method, BuildUri(route));
                request.Content = contentFactory?.Invoke();

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    timeoutCts.Token);

                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                return new ApiRawResponse(
                    response.IsSuccessStatusCode,
                    (int)response.StatusCode,
                    route,
                    body);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = CreateTimeoutException(route, timeout, exception);
            }
            catch (HttpRequestException exception)
            {
                lastException = CreateNetworkException(route, exception);
            }
            catch (IOException exception)
            {
                lastException = CreateNetworkException(route, exception);
            }

            if (attempt < attempts)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        if (lastException is ApiClientException apiClientException)
        {
            throw apiClientException;
        }

        throw new ApiClientException(
            $"Connexion API impossible sur {route}.",
            0,
            route,
            lastException?.Message ?? string.Empty,
            lastException);
    }

    /// <summary>
    /// Combine l'URL de base normalisée et la route demandée pour produire une URI absolue.
    /// </summary>
    private Uri BuildUri(string route)
    {
        var normalizedRoute = route.StartsWith("/", StringComparison.Ordinal)
            ? route
            : "/" + route;

        return new Uri(BaseUrl + normalizedRoute, UriKind.Absolute);
    }

    /// <summary>
    /// Normalise l'URL API : valeur de secours locale si vide, suppression du slash final,
    /// et ajout du schéma HTTP si aucun schéma n'est fourni.
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "http://127.0.0.1:5000";
        }

        var normalized = baseUrl.Trim().TrimEnd('/');

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = "http://" + normalized;
        }

        return normalized;
    }

    /// <summary>
    /// Traduit un code HTTP d'erreur en exception applicative lisible.
    /// Le corps brut est conservé pour le diagnostic technique.
    /// </summary>
    private static ApiClientException CreateException(
        int statusCode,
        string route,
        string body)
    {
        var message = ((HttpStatusCode)statusCode) switch
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
                => $"Erreur API HTTP {statusCode} sur {route}"
        };

        return new ApiClientException(message, statusCode, route, body);
    }

    /// <summary>
    /// Crée une exception explicite lorsqu'un appel API dépasse le délai autorisé.
    /// </summary>
    private static ApiClientException CreateTimeoutException(
        string route,
        TimeSpan timeout,
        Exception exception)
    {
        return new ApiClientException(
            $"Le serveur n'a pas répondu dans le délai prévu ({timeout.TotalSeconds:0} secondes) pour {route}. Vérifiez le Wi-Fi dépôt puis réessayez.",
            0,
            route,
            exception.Message,
            exception);
    }

    /// <summary>
    /// Crée une exception explicite lorsqu'un appel API échoue à cause du réseau.
    /// </summary>
    private static ApiClientException CreateNetworkException(
        string route,
        Exception exception)
    {
        return new ApiClientException(
            $"Connexion réseau interrompue pendant l'appel API {route}. Vérifiez le Wi-Fi dépôt puis réessayez.",
            0,
            route,
            exception.Message,
            exception);
    }
}

/// <summary>
/// Réponse HTTP brute retournée par l'API, avant transformation éventuelle en DTO métier.
/// </summary>
public sealed record ApiRawResponse(
    bool IsSuccess,
    int StatusCode,
    string Route,
    string Body);

/// <summary>
/// Exception applicative levée par <see cref="ApiClient"/> lorsqu'un appel API échoue.
/// Elle conserve le code HTTP, la route appelée et le corps de réponse pour faciliter le diagnostic.
/// </summary>
public sealed class ApiClientException : Exception
{
    public ApiClientException(
        string message,
        int statusCode,
        string route,
        string responseBody,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Route = route;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }

    public string Route { get; }

    public string ResponseBody { get; }
}
