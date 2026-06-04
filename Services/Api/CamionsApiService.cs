using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class CamionsApiService
{
    private const string RouteCamionsDisponibles = "/api/camions/disponibles";
    private const string ExpectedSchemaVersion = "1.3";

    private readonly ApiClient _apiClient;

    public CamionsApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<CamionDto>> GetCamionsDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetAsync<CamionsDisponiblesResponseDto>(
            RouteCamionsDisponibles,
            ApiTimeouts.DefaultGet,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException(
                "La réponse de l'API est vide pour la liste des camions disponibles.");
        }

        ValidateSchemaVersion(response.SchemaVersion);

        return response.Camions
            .Where(IsCamionExploitable)
            .Select(NormalizeCamion)
            .OrderBy(camion => camion.Immatriculation, StringComparer.OrdinalIgnoreCase)
            .ThenBy(camion => camion.LibelleCamion, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<IReadOnlyList<CamionDto>> ChargerCamionsDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        return GetCamionsDisponiblesAsync(cancellationToken);
    }

    private static void ValidateSchemaVersion(string? schemaVersion)
    {
        var version = schemaVersion?.Trim();

        if (!string.Equals(version, ExpectedSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Version du contrat camion incompatible. Version attendue : {ExpectedSchemaVersion}. Version reçue : {version ?? "<vide>"}.");
        }
    }

    private static bool IsCamionExploitable(CamionDto? camion)
    {
        return camion is not null
            && camion.EstActif
            && !string.IsNullOrWhiteSpace(camion.IdCamion)
            && !string.IsNullOrWhiteSpace(camion.CodeCamion);
    }

    private static CamionDto NormalizeCamion(CamionDto camion)
    {
        camion.IdCamion = camion.IdCamion.Trim();
        camion.CodeCamion = camion.CodeCamion.Trim();
        camion.LibelleCamion = camion.LibelleCamion?.Trim() ?? string.Empty;
        camion.Immatriculation = camion.Immatriculation?.Trim() ?? string.Empty;

        return camion;
    }
}
