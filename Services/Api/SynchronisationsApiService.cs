using System.Text.Json.Serialization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class SynchronisationsApiService
{
    private const string DateTourneeNonAutoriseeCode = "DATE_TOURNEE_NON_AUTORISEE";
    private const string DateTourneeExpireeCode = "DATE_TOURNEE_EXPIREE";

    private readonly ApiClient _apiClient;

    public SynchronisationsApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<OperationResult> PostSynchronisationAsync(
        SynchronisationTourneeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PostSynchronisationPayloadAsync(
            request,
            request.Lignes.Count,
            cancellationToken);
    }

    public Task<OperationResult> PostSynchronisationAsync(
        SynchronisationTourneeAvecTrajetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PostSynchronisationPayloadAsync(
            request,
            request.Lignes.Count,
            cancellationToken);
    }

    private async Task<OperationResult> PostSynchronisationPayloadAsync<TRequest>(
        TRequest request,
        int lignesEnvoyees,
        CancellationToken cancellationToken)
    {
        const string route = "/api/synchronisations";

        var response = await _apiClient.PostAsJsonAsync(
            route,
            request,
            ApiTimeouts.Synchronisation,
            cancellationToken);

        var apiResult = _apiClient.Deserialize<ApiSynchronisationResult>(response.Body);
        var code = GetCode(apiResult);

        if (response.IsSuccess)
        {
            return new OperationResult
            {
                Success = true,
                Code = string.IsNullOrWhiteSpace(code) ? "SUCCESS" : code,
                Message = GetMessage(apiResult) ?? "Synchronisation envoyée avec succès.",
                LignesEnvoyees = lignesEnvoyees
            };
        }

        if (response.StatusCode == 409)
        {
            if (IsDateTourneeNonAutorisee(code, response.Body))
            {
                return new OperationResult
                {
                    Success = false,
                    AlreadySynchronized = false,
                    Code = string.IsNullOrWhiteSpace(code) ? DateTourneeNonAutoriseeCode : code,
                    Message = GetMessage(apiResult)
                        ?? "La tournée ne correspond pas à la date autorisée par l'API. Rechargez les tournées du jour.",
                    TechnicalDetail = response.Body
                };
            }

            var isAlreadySynchronized =
                string.Equals(code, "SYNCHRONISATION_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "TOURNEE_ALREADY_SENT", StringComparison.OrdinalIgnoreCase);

            return new OperationResult
            {
                Success = false,
                AlreadySynchronized = isAlreadySynchronized,
                Code = code,
                Message = GetMessage(apiResult)
                    ?? "Cette tournée a déjà été synchronisée ou existe déjà côté API.",
                TechnicalDetail = response.Body
            };
        }

        if (response.StatusCode == 400)
        {
            if (IsDateTourneeNonAutorisee(code, response.Body))
            {
                return new OperationResult
                {
                    Success = false,
                    AlreadySynchronized = false,
                    Code = string.IsNullOrWhiteSpace(code) ? DateTourneeNonAutoriseeCode : code,
                    Message = GetMessage(apiResult)
                        ?? "La tournée ne correspond pas à la date autorisée par l'API. Rechargez les tournées du jour.",
                    TechnicalDetail = response.Body
                };
            }

            var details = ExtractValidationDetails(apiResult, response.Body);

            return new OperationResult
            {
                Success = false,
                Code = string.IsNullOrWhiteSpace(code) ? "VALIDATION_ERROR" : code,
                Message = string.IsNullOrWhiteSpace(details)
                    ? "Données invalides pour la synchronisation."
                    : $"Données invalides pour la synchronisation. {details}",
                TechnicalDetail = response.Body
            };
        }

        return new OperationResult
        {
            Success = false,
            Code = string.IsNullOrWhiteSpace(code) ? $"HTTP_{response.StatusCode}" : code,
            Message = GetMessage(apiResult)
                ?? $"Erreur API HTTP {response.StatusCode}. {response.Body}",
            TechnicalDetail = response.Body
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

    private static string GetCode(ApiSynchronisationResult? result)
    {
        if (!string.IsNullOrWhiteSpace(result?.Code))
        {
            return result.Code.Trim();
        }

        return result?.Statut?.Trim() ?? string.Empty;
    }

    private static string? GetMessage(ApiSynchronisationResult? result)
    {
        if (!string.IsNullOrWhiteSpace(result?.Message))
        {
            return result.Message.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result?.MessageRetour))
        {
            return result.MessageRetour.Trim();
        }

        if (!string.IsNullOrWhiteSpace(result?.Detail))
        {
            return result.Detail.Trim();
        }

        return string.IsNullOrWhiteSpace(result?.Title)
            ? null
            : result.Title.Trim();
    }

    private static bool IsDateTourneeNonAutorisee(string? code, string? body)
    {
        return string.Equals(code, DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(code, DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrWhiteSpace(body)
                   && (body.Contains(DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
                       || body.Contains(DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase)
                       || body.Contains("dateTourneeAutorisee", StringComparison.OrdinalIgnoreCase)
                       || body.Contains("dateTourneeRecue", StringComparison.OrdinalIgnoreCase)));
    }

    private static string ExtractValidationDetails(
        ApiSynchronisationResult? apiResult,
        string rawBody)
    {
        if (apiResult?.Errors is { Count: > 0 })
        {
            return string.Join(Environment.NewLine, apiResult.Errors);
        }

        if (apiResult?.Erreurs is { Count: > 0 })
        {
            return string.Join(
                Environment.NewLine,
                apiResult.Erreurs.Select(error =>
                    string.IsNullOrWhiteSpace(error.Champ)
                        ? error.Message
                        : $"{error.Champ} : {error.Message}"));
        }

        return rawBody;
    }

    private sealed class ApiSynchronisationResult
    {
        [JsonPropertyName("statut")]
        public string? Statut { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("messageRetour")]
        public string? MessageRetour { get; set; }

        [JsonPropertyName("detail")]
        public string? Detail { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("erreurs")]
        public List<ApiValidationError>? Erreurs { get; set; }
    }

    private sealed class ApiValidationError
    {
        [JsonPropertyName("champ")]
        public string? Champ { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
