using System.Globalization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class LivreursApiService
{
    private readonly ApiClient _apiClient;

    public LivreursApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

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

    public Task<IReadOnlyList<LivreurDto>> ChargerLivreursAsync(
        CancellationToken cancellationToken = default)
    {
        return GetLivreursAsync(cancellationToken);
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
