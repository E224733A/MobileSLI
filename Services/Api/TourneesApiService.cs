using System.Globalization;
using System.Text.Json;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class TourneesApiService
{
    private readonly ApiClient _apiClient;

    public TourneesApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<TourneeJourDto> GetTourneeJourAsync(
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

        var route = _apiClient.BuildRoute(
            "/api/tournees/jour",
            new Dictionary<string, string?>
            {
                ["dateTournee"] = _apiClient.FormatDate(dateTournee),
                ["codeTournee"] = codeTournee.Trim(),
                ["codeLivreur"] = codeLivreur.Trim()
            });

        var tournee = await _apiClient.GetAsync<TourneeJourDto>(
            route,
            cancellationToken);

        if (tournee is null)
        {
            throw new InvalidOperationException(
                "La réponse de l'API est vide pour le chargement de la tournée.");
        }

        return tournee;
    }

    public async Task<TourneeJourDto> GetTourneeJourAsync(
        string dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(
                dateTournee,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            throw new ArgumentException("La date de tournée est invalide.", nameof(dateTournee));
        }

        return await GetTourneeJourAsync(
            parsedDate,
            codeTournee,
            codeLivreur,
            cancellationToken);
    }

    public Task<TourneeJourDto> ChargerTourneeDuJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeJourAsync(
            dateTournee,
            codeTournee,
            codeLivreur,
            cancellationToken);
    }

    public Task<TourneeJourDto> ChargerTourneeAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeJourAsync(
            dateTournee,
            codeTournee,
            codeLivreur,
            cancellationToken);
    }

    /*
     * Cette méthode est prévue seulement si l'API ajoute plus tard
     * une route de liste des tournées.
     *
     * La route confirmée actuellement charge une tournée complète avec :
     * dateTournee + codeTournee + codeLivreur.
     */
    public async Task<IReadOnlyList<TourneeResumeDto>> GetTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codeLivreur))
        {
            throw new ArgumentException("Le code livreur est obligatoire.", nameof(codeLivreur));
        }

        var route = _apiClient.BuildRoute(
            "/api/tournees/jour",
            new Dictionary<string, string?>
            {
                ["dateTournee"] = _apiClient.FormatDate(dateTournee),
                ["codeLivreur"] = codeLivreur.Trim()
            });

        using var document = await GetJsonDocumentAsync(route, cancellationToken);

        var tournees = ExtractList<TourneeResumeDto>(
            document.RootElement,
            "tournees");

        return tournees
            .Where(tournee => !string.IsNullOrWhiteSpace(tournee.CodeTournee))
            .OrderBy(tournee => TryParseInt(tournee.CodeTournee))
            .ThenBy(tournee => tournee.LibelleTournee)
            .ToList();
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(
        string route,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetRawAsync(route, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Erreur API HTTP {response.StatusCode} sur {route}. {response.Body}");
        }

        if (string.IsNullOrWhiteSpace(response.Body))
        {
            throw new InvalidOperationException("La réponse de l'API est vide.");
        }

        return JsonDocument.Parse(response.Body);
    }

    private static List<T> ExtractList<T>(
        JsonElement root,
        string preferredPropertyName)
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
                var singleItem = property.Deserialize<T>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return singleItem is null ? [] : [singleItem];
            }
        }

        var item = root.Deserialize<T>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            });

        return item is null ? [] : [item];
    }

    private static List<T> DeserializeList<T>(JsonElement element)
    {
        return element.Deserialize<List<T>>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
    }

    private static int TryParseInt(string? value)
    {
        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var number)
            ? number
            : int.MaxValue;
    }
}
