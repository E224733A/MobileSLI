using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

/// <summary>
/// Service dédié aux appels relatifs aux tournées. Cette version ajuste les
/// délais d'appel pour la récupération des listes de tournées et des
/// tournées complètes afin de respecter les nouvelles valeurs de timeout
/// définies dans ApiTimeouts. Elle n'envoie plus de date de tournée lors des
/// appels, conformément au contrat de l'API.
/// </summary>
public sealed class TourneesApiService
{
    private readonly ApiClient _apiClient;

    public TourneesApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Date de tournée renvoyée par le dernier appel à l'API. Utile pour
    /// connaître la date métier prise en compte par l'API sans l'envoyer dans
    /// les requêtes.
    /// </summary>
    public DateTime? LastDateTourneeApi { get; private set; }

    /// <summary>
    /// Récupère la liste des tournées disponibles pour un livreur. Ne transmet
    /// plus de date à l'API. Utilise ApiTimeouts.TourneesDisponibles (120s).
    /// </summary>
    /// <param name="codeLivreur">Code du livreur</param>
    /// <param name="cancellationToken">Jeton d'annulation</param>
    /// <returns>Liste triée des tournées disponibles</returns>
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
            ApiTimeouts.TourneesDisponibles,
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

    /// <summary>
    /// Surcharge pour compatibilité interne. Le paramètre dateTournee est
    /// ignoré car l'API calcule la date métier.
    /// </summary>
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

    /// <summary>
    /// Récupère la tournée complète du jour pour un code tournée et un livreur. Le
    /// timeout est défini à ApiTimeouts.ChargementTournee (180s) et une
    /// tentative supplémentaire sera effectuée après un délai de 1,5s.
    /// </summary>
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

    public Task<TourneeJourDto> GetTourneeJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeJourAsync(codeTournee, codeLivreur, cancellationToken);
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