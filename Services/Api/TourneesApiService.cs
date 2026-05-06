using System.Globalization;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

public sealed class TourneesApiService
{
    private readonly ApiClient _apiClient;

    public TourneesApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /*
     * Écran "Choix de tournée"
     *
     * Appel léger :
     * GET /api/tournees/disponibles?dateTournee=YYYY-MM-DD&codeLivreur=XX
     *
     * Réponse attendue :
     * [
     *   { "codeTournee": "3001", "libelleTournee": "MDR VENDEE" }
     * ]
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
            "/api/tournees/disponibles",
            new Dictionary<string, string?>
            {
                ["dateTournee"] = _apiClient.FormatDate(dateTournee),
                ["codeLivreur"] = codeLivreur.Trim()
            });

        var tournees = await _apiClient.GetAsync<List<TourneeResumeDto>>(
            route,
            cancellationToken);

        return (tournees ?? [])
            .Where(tournee => !string.IsNullOrWhiteSpace(tournee.CodeTournee))
            .Select(tournee =>
            {
                tournee.DateTournee = dateTournee.Date;
                return tournee;
            })
            .OrderBy(tournee => TryParseInt(tournee.CodeTournee))
            .ThenBy(tournee => tournee.LibelleTournee)
            .ToList();
    }

    public Task<IReadOnlyList<TourneeResumeDto>> ChargerTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(dateTournee, codeLivreur, cancellationToken);
    }

    /*
     * Écran "Confirmation de tournée"
     *
     * Une fois la tournée sélectionnée, on charge la tournée complète.
     */
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

        NormalizeTournee(tournee);

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

    private static void NormalizeTournee(TourneeJourDto tournee)
    {
        tournee.Livreur ??= new LivreurDto();
        tournee.Chargement ??= new ChargementDto();
        tournee.ArticlesSaisissables ??= [];
        tournee.Lignes ??= [];

        foreach (var ligne in tournee.Lignes)
        {
            ligne.Client ??= new ClientDto();
            ligne.PointLivraison ??= new PointLivraisonDto();
            ligne.Tournee ??= new TourneeInfoDto();
            ligne.Retour ??= new RetourInfoDto();
            ligne.InfosLivreur ??= new InfosLivreurDto();
            ligne.Saisie ??= new SaisieMobileDto();
            ligne.Saisie.Quantites ??= [];
        }
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
