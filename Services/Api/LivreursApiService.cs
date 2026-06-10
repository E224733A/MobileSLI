using System.Globalization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

/// <summary>
/// Service d'accès aux livreurs exposés par l'API.
/// Il prépare une liste exploitable par l'écran d'identification livreur en filtrant les entrées sans code
/// et en appliquant un tri stable sur les codes numériques.
/// </summary>
public sealed class LivreursApiService
{
    private readonly ApiClient _apiClient;

    public LivreursApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Charge les livreurs depuis l'API et retire les entrées sans code livreur.
    /// Le code livreur est obligatoire car il sert d'identifiant fonctionnel pour la tournée et la synchronisation.
    /// </summary>
    public async Task<IReadOnlyList<LivreurDto>> GetLivreursAsync(
        CancellationToken cancellationToken = default)
    {
        const string route = "/api/livreurs";

        var livreurs = await _apiClient.GetAsync<List<LivreurDto>>(
            route,
            cancellationToken);

        return (livreurs ?? [])
            .Where(livreur => !string.IsNullOrWhiteSpace(livreur.CodeLivreur))
            .OrderBy(livreur => TryParseInt(livreur.CodeLivreur))
            .ThenBy(livreur => livreur.NomLivreur)
            .ToList();
    }

    /// <summary>
    /// Alias conservé pour les écrans ou services qui utilisent le vocabulaire métier "charger".
    /// </summary>
    public Task<IReadOnlyList<LivreurDto>> ChargerLivreursAsync(
        CancellationToken cancellationToken = default)
    {
        return GetLivreursAsync(cancellationToken);
    }

    /// <summary>
    /// Convertit un code livreur en entier pour obtenir un tri naturel lorsque les codes sont numériques.
    /// Les codes non numériques sont placés en fin de liste plutôt que de provoquer une erreur d'affichage.
    /// </summary>
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
