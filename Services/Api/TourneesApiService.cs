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

    public DateTime? LastDateTourneeApi { get; private set; }

    /*
     * Écran "Choix de tournée"
     *
     * Contrat API final :
     * GET /api/tournees/disponibles?codeLivreur=XX
     *
     * La date de tournée n'est plus envoyée par le mobile.
     * Elle est calculée côté API avec la date métier Europe/Paris.
     *
     * Réponse finale v1.2 :
     * {
     *   "schemaVersion": "1.2",
     *   "dateTournee": "2026-05-21",
     *   "dateModifiable": false,
     *   "livreur": { ... },
     *   "tournees": [ ... ]
     * }
     *
     * Compatibilité conservée temporairement avec l'ancien format :
     * [
     *   { "codeTournee": "3001", "libelleTournee": "MDR VENDEE" }
     * ]
     */
    public async Task<IReadOnlyList<TourneeResumeDto>> GetTourneesDuJourAsync(
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
                ["codeLivreur"] = codeLivreur.Trim()
            });

        var response = await _apiClient.GetRawAsync(
            route,
            ApiTimeouts.ChargementTournee,
            retryCount: 0,
            retryDelay: TimeSpan.Zero,
            cancellationToken);

        if (!response.IsSuccess)
        {
            throw new ApiClientException(
                $"Erreur API HTTP {response.StatusCode} sur {route}. Réponse API : {response.Body}",
                response.StatusCode,
                route,
                response.Body);
        }

        var envelope = _apiClient.Deserialize<TourneesDisponiblesResponseDto>(response.Body);

        IReadOnlyList<TourneeResumeDto>? tournees = null;
        var effectiveDate = DateTime.Today;

        if (envelope is not null)
        {
            tournees = envelope.Tournees;
            effectiveDate = envelope.DateTournee == default
                ? DateTime.Today
                : envelope.DateTournee.Date;
        }

        if (tournees is null)
        {
            tournees = _apiClient.Deserialize<List<TourneeResumeDto>>(response.Body) ?? [];
        }

        LastDateTourneeApi = effectiveDate;

        return tournees
            .Where(tournee => !string.IsNullOrWhiteSpace(tournee.CodeTournee))
            .Select(tournee =>
            {
                tournee.DateTournee = effectiveDate;
                return tournee;
            })
            .OrderBy(tournee => TryParseInt(tournee.CodeTournee))
            .ThenBy(tournee => tournee.LibelleTournee)
            .ToList();
    }

    /*
     * Surcharge conservée pour compatibilité interne.
     * Le paramètre dateTournee est volontairement ignoré, car l'API finale refuse
     * toute date dans l'URL.
     */
    public Task<IReadOnlyList<TourneeResumeDto>> GetTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(codeLivreur, cancellationToken);
    }

    public Task<IReadOnlyList<TourneeResumeDto>> ChargerTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(codeLivreur, cancellationToken);
    }

    /*
     * Écran "Confirmation de tournée"
     *
     * Contrat API final :
     * GET /api/tournees/jour?codeTournee=XXXX&codeLivreur=XX
     *
     * La date de tournée n'est plus envoyée par le mobile.
     * Elle est calculée côté API avec la date métier Europe/Paris.
     *
     * Timeout réseau : 60 secondes.
     * Retry automatique : 1 seule nouvelle tentative après 1,5 seconde.
     */
    public async Task<TourneeJourDto> GetTourneeJourAsync(
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
                ["codeTournee"] = codeTournee.Trim(),
                ["codeLivreur"] = codeLivreur.Trim()
            });

        var tournee = await _apiClient.GetAsync<TourneeJourDto>(
            route,
            ApiTimeouts.ChargementTournee,
            ApiTimeouts.ChargementTourneeRetryCount,
            ApiTimeouts.ChargementTourneeRetryDelay,
            cancellationToken);

        if (tournee is null)
        {
            throw new InvalidOperationException(
                "La réponse de l'API est vide pour le chargement de la tournée.");
        }

        NormalizeTournee(tournee);

        if (tournee.DateTournee != default)
        {
            LastDateTourneeApi = tournee.DateTournee.Date;
        }

        return tournee;
    }

    /*
     * Surcharge conservée pour compatibilité interne.
     * Le paramètre dateTournee est volontairement ignoré.
     */
    public Task<TourneeJourDto> GetTourneeJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeJourAsync(codeTournee, codeLivreur, cancellationToken);
    }

    /*
     * Surcharge conservée pour compatibilité avec d'anciens appels internes.
     * Le paramètre dateTournee est validé uniquement pour éviter un appel incohérent,
     * mais il n'est jamais envoyé à l'API.
     */
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
                out _))
        {
            throw new ArgumentException("La date de tournée est invalide.", nameof(dateTournee));
        }

        return await GetTourneeJourAsync(
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
