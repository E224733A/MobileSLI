using System.Net.Http.Json;
using System.Text.Json;
using TourneesMobile.Models;

namespace TourneesMobile.Services;

public sealed class ApiService
{
    private readonly SettingsService _settings;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ApiService(SettingsService settings)
    {
        _settings = settings;
    }

    private HttpClient CreateClient() => new()
    {
        BaseAddress = new Uri(_settings.ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<TourneeMobileDto> GetTourneeJourAsync(string dateTournee, string codeTournee, string codeLivreur)
    {
        using var client = CreateClient();

        var url =
            $"/api/tournees/jour?dateTournee={Uri.EscapeDataString(dateTournee)}" +
            $"&codeTournee={Uri.EscapeDataString(codeTournee)}" +
            $"&codeLivreur={Uri.EscapeDataString(codeLivreur)}";

        using var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new ApiException($"Chargement impossible ({(int)response.StatusCode}) : {body}");

        var dto = JsonSerializer.Deserialize<TourneeMobileDto>(body, JsonOptions);
        return dto ?? throw new ApiException("L'API a renvoyé une tournée vide ou invalide.");
    }

    public async Task<SynchronisationResponse> SynchroniserTourneeAsync(SynchronisationTourneeRequest request)
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/synchronisations", request, JsonOptions);
        var body = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<SynchronisationResponse>(body, JsonOptions);

        if (result is not null)
            return result;

        if (!response.IsSuccessStatusCode)
        {
            return new SynchronisationResponse
            {
                Success = false,
                Message = $"Synchronisation refusée ({(int)response.StatusCode})",
                Errors = [body]
            };
        }

        throw new ApiException("L'API a renvoyé une réponse de synchronisation invalide.");
    }
}

public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception inner) : base(message, inner) { }
}
