using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MobileSLI.Models;

namespace MobileSLI.Services.Api;

/// <summary>
/// Service dédié aux appels API liés aux tournées.
/// Le mobile ne transmet pas la date métier dans les requêtes : l'API décide elle-même
/// de la date autorisée afin d'éviter les écarts entre téléphone, serveur et règles d'exploitation.
/// </summary>
public sealed class TourneesApiService
{
    private readonly ApiClient _apiClient;

    public TourneesApiService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <summary>
    /// Date de tournée renvoyée par le dernier appel API.
    /// Elle sert à mémoriser la date métier réellement retenue côté serveur sans l'imposer depuis le mobile.
    /// </summary>
    public DateTime? LastDateTourneeApi { get; private set; }

    /// <summary>
    /// Récupère les tournées disponibles pour un livreur.
    /// Le code livreur est le seul filtre envoyé, car la date de tournée est calculée par l'API.
    /// </summary>
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
    /// Surcharge de compatibilité : le paramètre dateTournee est volontairement ignoré.
    /// Cette décision évite que le mobile force une date différente de celle autorisée par l'API.
    /// </summary>
    public Task<IReadOnlyList<TourneeResumeDto>> GetTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(codeLivreur, cancellationToken);
    }

    /// <summary>
    /// Alias métier conservé pour les appels existants de chargement de tournées.
    /// </summary>
    public Task<IReadOnlyList<TourneeResumeDto>> ChargerTourneesDuJourAsync(
        DateTime dateTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneesDuJourAsync(codeLivreur, cancellationToken);
    }

    /// <summary>
    /// Récupère le détail complet d'une tournée pour un livreur.
    /// Cet appel utilise un timeout plus long et une tentative supplémentaire, car le chargement complet
    /// contient toutes les lignes, les articles saisissables et les données nécessaires à la livraison.
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

    /// <summary>
    /// Surcharge de compatibilité : la date reçue n'est pas envoyée à l'API.
    /// </summary>
    public Task<TourneeJourDto> GetTourneeJourAsync(
        DateTime dateTournee,
        string codeTournee,
        string codeLivreur,
        CancellationToken cancellationToken = default)
    {
        return GetTourneeJourAsync(codeTournee, codeLivreur, cancellationToken);
    }

    /// <summary>
    /// Surcharge de compatibilité avec les anciens appels qui fournissaient la date sous forme de texte.
    /// La date est seulement validée pour détecter une erreur d'appel, puis elle n'est pas transmise à l'API.
    /// </summary>
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

    /// <summary>
    /// Alias métier conservé pour le chargement de la tournée du jour.
    /// </summary>
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

    /// <summary>
    /// Alias métier conservé pour les écrans ou services utilisant le nom historique ChargerTourneeAsync.
    /// </summary>
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

    /// <summary>
    /// Sécurise le DTO de tournée reçu de l'API en initialisant les sous-objets et collections nulles.
    /// Cette normalisation évite des contrôles défensifs répétés dans les ViewModels et dans les pages MAUI.
    /// </summary>
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

    /// <summary>
    /// Convertit un code tournée en entier pour obtenir un tri naturel lorsque les codes sont numériques.
    /// Les codes non numériques sont placés en fin de liste plutôt que de bloquer le chargement.
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
