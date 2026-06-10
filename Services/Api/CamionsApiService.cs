using MobileSLI.Models;

namespace MobileSLI.Services.Api;

/// <summary>
/// Service d'accès à la liste des camions disponibles côté API.
/// Il applique le contrat attendu par le mobile, filtre les camions non exploitables
/// et normalise les chaînes avant l'affichage ou la sélection par le livreur.
/// </summary>
public sealed class CamionsApiService
{
    private const string RouteCamionsDisponibles = "/api/camions/disponibles";

    /// <summary>
    /// Version du contrat camion attendue par l'application mobile.
    /// Une version différente est bloquante afin d'éviter d'afficher ou d'envoyer des données incompatibles.
    /// </summary>
    private const string ExpectedSchemaVersion = "1.3";

    private readonly ApiClient _apiClient;

    public CamionsApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Charge les camions actifs disponibles depuis l'API.
    /// Les camions sans identifiant ou sans code sont ignorés, car ils ne peuvent pas être rattachés proprement
    /// à une synchronisation de tournée.
    /// </summary>
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

    /// <summary>
    /// Alias conservé pour les écrans ou services qui utilisent le vocabulaire métier "charger".
    /// </summary>
    public Task<IReadOnlyList<CamionDto>> ChargerCamionsDisponiblesAsync(
        CancellationToken cancellationToken = default)
    {
        return GetCamionsDisponiblesAsync(cancellationToken);
    }

    /// <summary>
    /// Vérifie que la réponse API respecte la version de schéma attendue par le mobile.
    /// Cette vérification protège le contrat JSON avant toute utilisation fonctionnelle de la liste des camions.
    /// </summary>
    private static void ValidateSchemaVersion(string? schemaVersion)
    {
        var version = schemaVersion?.Trim();

        if (!string.Equals(version, ExpectedSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Version du contrat camion incompatible. Version attendue : {ExpectedSchemaVersion}. Version reçue : {version ?? "<vide>"}.");
        }
    }

    /// <summary>
    /// Détermine si un camion reçu de l'API est exploitable par le mobile.
    /// Un camion inactif ou sans identifiants fiables est exclu pour éviter une synchronisation ambiguë.
    /// </summary>
    private static bool IsCamionExploitable(CamionDto? camion)
    {
        return camion is not null
            && camion.EstActif
            && !string.IsNullOrWhiteSpace(camion.IdCamion)
            && !string.IsNullOrWhiteSpace(camion.CodeCamion);
    }

    /// <summary>
    /// Normalise les chaînes reçues de l'API avant usage local.
    /// Cela évite que des espaces parasites perturbent le tri, l'affichage ou les comparaisons.
    /// </summary>
    private static CamionDto NormalizeCamion(CamionDto camion)
    {
        camion.IdCamion = camion.IdCamion.Trim();
        camion.CodeCamion = camion.CodeCamion.Trim();
        camion.LibelleCamion = camion.LibelleCamion?.Trim() ?? string.Empty;
        camion.Immatriculation = camion.Immatriculation?.Trim() ?? string.Empty;

        return camion;
    }
}
