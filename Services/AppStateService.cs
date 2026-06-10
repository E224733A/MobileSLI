using System;
using System.Collections.Generic;
using MobileSLI.Models;

namespace MobileSLI.Services;

/// <summary>
/// Service d'état courant conservé en mémoire pendant la session mobile.
/// Il relie les écrans entre eux : livreur, camion, tournée sélectionnée, ligne courante,
/// résultat de synchronisation, date métier API et cache journalier des tournées disponibles.
/// </summary>
public sealed class AppStateService
{
    // Cache mémoire uniquement : il accélère le retour à la liste des tournées sans modifier SQLite.
    private readonly List<TourneeResumeDto> _tourneesDisponiblesCache = new();

    /// <summary>
    /// Code livreur associé au cache des tournées disponibles.
    /// </summary>
    public string? TourneesDisponiblesCacheCodeLivreur { get; private set; }

    /// <summary>
    /// Date locale à laquelle le cache des tournées disponibles a été créé.
    /// </summary>
    public DateTime? TourneesDisponiblesCacheDate { get; private set; }

    private LivreurDto? _currentLivreur;

    /// <summary>
    /// Livreur actuellement sélectionné.
    /// Un changement de livreur invalide le cache, le camion, le trajet et la tournée courante
    /// afin d'éviter de mélanger les données de deux livreurs différents.
    /// </summary>
    public LivreurDto? CurrentLivreur
    {
        get => _currentLivreur;
        set
        {
            var oldCode = _currentLivreur?.CodeLivreur;
            var newCode = value?.CodeLivreur;

            if (!string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
            {
                ClearTourneesDisponiblesCache();
                ClearTrajet();
                SelectedTournee = null;
                CurrentTourneeId = 0;
                SelectedLigneId = 0;
            }

            _currentLivreur = value;
        }
    }

    /// <summary>
    /// Camion sélectionné pour la tournée courante.
    /// Cette valeur est aussi restaurable depuis SQLite via ApplyTrajetFromTournee.
    /// </summary>
    public CamionDto? CurrentCamion { get; set; }

    /// <summary>
    /// Kilométrage de départ saisi au moment du choix camion.
    /// </summary>
    public int? KilometrageDepart { get; set; }

    /// <summary>
    /// Date et heure locales de validation du départ.
    /// </summary>
    public DateTime? DateDepartMobile { get; set; }

    /// <summary>
    /// Kilométrage d'arrivée saisi avant l'envoi de la tournée.
    /// </summary>
    public int? KilometrageArrivee { get; set; }

    /// <summary>
    /// Date et heure locales de validation de l'arrivée.
    /// </summary>
    public DateTime? DateArriveeMobile { get; set; }

    /// <summary>
    /// Tournée résumée actuellement sélectionnée dans le flux de chargement.
    /// </summary>
    public TourneeResumeDto? SelectedTournee { get; set; }

    /// <summary>
    /// Identifiant SQLite de la tournée actuellement chargée localement.
    /// </summary>
    public int CurrentTourneeId { get; set; }

    /// <summary>
    /// Identifiant SQLite de la ligne actuellement ouverte dans l'écran détail.
    /// </summary>
    public int SelectedLigneId { get; set; }

    /// <summary>
    /// Dernier résultat de synchronisation, utilisé par les écrans de résultat ou d'erreur.
    /// </summary>
    public OperationResult? LastSyncResult { get; set; }

    /*
     * Date métier renvoyée par une route API existante.
     * Elle évite de s'appuyer uniquement sur DateTime.Today côté téléphone
     * dès que l'application a reçu une réponse de l'API.
     */
    public DateTime? DateTourneeAutorisee { get; set; }

    /*
     * Empêche la popup de reprise d'être réaffichée plusieurs fois dans
     * la même session applicative si AccueilPage ou AccueilViewModel sont
     * recréés.
     *
     * La valeur revient à false après fermeture réelle de l'application
     * puis relance, ce qui permet de proposer la reprise uniquement dans
     * ce cas.
     */
    public bool HasCheckedActiveTourneeOnStartup { get; set; }

    /// <summary>
    /// Réinitialise uniquement les données camion/trajet du flux courant.
    /// Cette méthode ne supprime rien dans SQLite : elle nettoie seulement l'état mémoire.
    /// </summary>
    public void ClearTrajet()
    {
        CurrentCamion = null;
        KilometrageDepart = null;
        DateDepartMobile = null;
        KilometrageArrivee = null;
        DateArriveeMobile = null;
    }

    /// <summary>
    /// Recharge en mémoire les informations de trajet persistées dans une tournée SQLite.
    /// Cette méthode est utilisée pour reprendre une tournée après navigation ou redémarrage,
    /// sans perdre le camion et les kilométrages déjà saisis.
    /// </summary>
    public void ApplyTrajetFromTournee(LocalTournee tournee)
    {
        ArgumentNullException.ThrowIfNull(tournee);

        CurrentCamion = HasPersistedCamion(tournee)
            ? new CamionDto
            {
                IdCamion = tournee.IdCamion?.Trim() ?? string.Empty,
                CodeCamion = tournee.CodeCamion?.Trim() ?? string.Empty,
                LibelleCamion = tournee.LibelleCamion?.Trim() ?? string.Empty,
                Immatriculation = tournee.Immatriculation?.Trim() ?? string.Empty,
                EstActif = true
            }
            : null;

        KilometrageDepart = tournee.KilometrageDepart;
        KilometrageArrivee = tournee.KilometrageArrivee;
        DateDepartMobile = tournee.DateDepartMobile;
        DateArriveeMobile = tournee.DateArriveeMobile;
    }

    /// <summary>
    /// Détermine si une tournée locale contient assez d'informations pour reconstituer un camion sélectionné.
    /// L'immatriculation seule est acceptée pour couvrir certains anciens enregistrements ou données partielles.
    /// </summary>
    private static bool HasPersistedCamion(LocalTournee tournee)
    {
        return !string.IsNullOrWhiteSpace(tournee.IdCamion)
               || !string.IsNullOrWhiteSpace(tournee.CodeCamion)
               || !string.IsNullOrWhiteSpace(tournee.Immatriculation);
    }

    /// <summary>
    /// Vérifie si le cache des tournées disponibles correspond au livreur demandé et à la date du jour.
    /// </summary>
    public bool HasTourneesDisponiblesCacheForToday(string codeLivreur)
    {
        return _tourneesDisponiblesCache.Count > 0
               && string.Equals(TourneesDisponiblesCacheCodeLivreur, codeLivreur, StringComparison.OrdinalIgnoreCase)
               && TourneesDisponiblesCacheDate?.Date == DateTime.Today;
    }

    /// <summary>
    /// Retourne le cache mémoire des tournées disponibles en lecture seule.
    /// </summary>
    public IReadOnlyList<TourneeResumeDto> GetTourneesDisponiblesCache() => _tourneesDisponiblesCache;

    /// <summary>
    /// Enregistre en mémoire la liste des tournées disponibles pour un livreur.
    /// La date du cache est fixée au jour courant pour éviter une réutilisation le lendemain.
    /// </summary>
    public void SaveTourneesDisponiblesCache(string codeLivreur, IEnumerable<TourneeResumeDto> tournees)
    {
        _tourneesDisponiblesCache.Clear();
        _tourneesDisponiblesCache.AddRange(tournees);
        TourneesDisponiblesCacheCodeLivreur = codeLivreur;
        TourneesDisponiblesCacheDate = DateTime.Today;
    }

    /// <summary>
    /// Vide le cache mémoire des tournées disponibles et ses métadonnées.
    /// </summary>
    public void ClearTourneesDisponiblesCache()
    {
        _tourneesDisponiblesCache.Clear();
        TourneesDisponiblesCacheCodeLivreur = null;
        TourneesDisponiblesCacheDate = null;
    }

    /// <summary>
    /// Supprime le cache journalier si la date enregistrée n'est plus la date du jour.
    /// À appeler avant d'utiliser le cache pour éviter de présenter d'anciennes tournées au livreur.
    /// </summary>
    public void ClearDailyApiCacheIfNeeded()
    {
        if (TourneesDisponiblesCacheDate?.Date != DateTime.Today)
        {
            ClearTourneesDisponiblesCache();
        }
    }
}
