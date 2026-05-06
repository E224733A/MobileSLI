using System.Text.Json.Serialization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class SynchronisationsApiService
{
    private readonly ApiClient _apiClient;

    public SynchronisationsApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<OperationResult> PostSynchronisationAsync(
        SynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        const string route = "/api/synchronisations";

        var response = await _apiClient.PostAsJsonAsync(
            route,
            request,
            cancellationToken);

        var apiResult = _apiClient.Deserialize<ApiSynchronisationResult>(response.Body);

        if (response.IsSuccess)
        {
            return new OperationResult
            {
                Success = true,
                Message = apiResult?.Message ?? "Synchronisation envoyée avec succès."
            };
        }

        if (response.StatusCode == 409)
        {
            return new OperationResult
            {
                Success = false,
                Message = apiResult?.Message
                    ?? "Cette tournée a déjà été synchronisée ou existe déjà côté API."
            };
        }

        if (response.StatusCode == 400)
        {
            var details = apiResult?.Errors is { Count: > 0 }
                ? string.Join(Environment.NewLine, apiResult.Errors)
                : response.Body;

            return new OperationResult
            {
                Success = false,
                Message = $"Données invalides pour la synchronisation. {details}"
            };
        }

        return new OperationResult
        {
            Success = false,
            Message = apiResult?.Message
                ?? $"Erreur API HTTP {response.StatusCode}. {response.Body}"
        };
    }

    public Task<OperationResult> SynchroniserTourneeAsync(
        SynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostSynchronisationAsync(request, cancellationToken);
    }

    public Task<OperationResult> EnvoyerSynchronisationAsync(
        SynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostSynchronisationAsync(request, cancellationToken);
    }

    private sealed class ApiSynchronisationResult
    {
        [JsonPropertyName("statut")]
        public string? Statut { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }
    }
}
