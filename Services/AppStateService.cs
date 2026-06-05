using System;
using System.Collections.Generic;
using MobileSLI.Models;

namespace MobileSLI.Services;

/// <summary>
/// Service qui centralise l'état courant de l'application mobile.
/// Cette version ajoute un cache mémoire journalier pour les tournées disponibles
/// et invalide ce cache automatiquement lorsque la date du jour change ou lorsque
/// le livreur sélectionné change. Le cache est uniquement en mémoire et n'est
/// jamais persistant afin de respecter le schéma SQLite existant.
/// </summary>
public sealed class AppStateService
{
    // Cache des tournées disponibles pour un livreur donné et une journée.
    private readonly List<TourneeResumeDto> _tourneesDisponiblesCache = new();

    /// <summary>
    /// Code livreur associé au cache des tournées disponibles.
    /// </summary>
    public string? TourneesDisponiblesCacheCodeLivreur { get; private set; }

    /// <summary>
    /// Date locale (DateTime.Today) à laquelle le cache des tournées disponibles a été créé.
    /// </summary>
    public DateTime? TourneesDisponiblesCacheDate { get; private set; }

    private LivreurDto? _currentLivreur;

    /// <summary>
    /// Livreur actuellement sélectionné. Changer le code livreur invalide le cache des tournées
    /// et réinitialise le camion/trajet courant pour éviter de mélanger deux flux livreur.
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
    /// Camion sélectionné pour le flux courant.
    /// </summary>
    public CamionDto? CurrentCamion { get; set; }

    /// <summary>
    /// Kilométrage départ saisi côté mobile. La validation stricte est prévue au lot 3.
    /// </summary>
    public int? KilometrageDepart { get; set; }

    /// <summary>
    /// Date de validation du départ mobile. Elle sera alimentée dans un lot ultérieur.
    /// </summary>
    public DateTime? DateDepartMobile { get; set; }

    /// <summary>
    /// Kilométrage arrivée saisi côté mobile. La validation stricte est prévue au lot 5.
    /// </summary>
    public int? KilometrageArrivee { get; set; }

    /// <summary>
    /// Date de validation de l'arrivée mobile. Elle sera alimentée dans un lot ultérieur.
    /// </summary>
    public DateTime? DateArriveeMobile { get; set; }

    /// <summary>
    /// Tournée résumée actuellement sélectionnée.
    /// </summary>
    public TourneeResumeDto? SelectedTournee { get; set; }

    /// <summary>
    /// Identifiant interne de la tournée courante chargée en local.
    /// </summary>
    public int CurrentTourneeId { get; set; }

    /// <summary>
    /// Identifiant interne de la ligne actuellement sélectionnée.
    /// </summary>
    public int SelectedLigneId { get; set; }

    /// <summary>
    /// Résultat de la dernière synchronisation exécutée.
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
    /// Réinitialise les données camion/trajet temporaires du flux courant.
    /// Cette méthode ne touche ni à SQLite ni à la synchronisation finale.
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
    /// Recharge les données trajet persistées dans une tournée SQLite locale.
    /// Cette méthode ne modifie pas le livreur, la tournée sélectionnée ni les lignes.
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

    private static bool HasPersistedCamion(LocalTournee tournee)
    {
        return !string.IsNullOrWhiteSpace(tournee.IdCamion)
               || !string.IsNullOrWhiteSpace(tournee.CodeCamion)
               || !string.IsNullOrWhiteSpace(tournee.Immatriculation);
    }

    /// <summary>
    /// Détermine si un cache de tournées disponibles est présent pour le livreur spécifié
    /// et pour la journée courante. La comparaison de date est effectuée sur DateTime.Today
    /// afin de ne pas dépendre de l'heure.
    /// </summary>
    /// <param name="codeLivreur">Code du livreur.</param>
    /// <returns>True si un cache valide existe ; sinon false.</returns>
    public bool HasTourneesDisponiblesCacheForToday(string codeLivreur)
    {
        return _tourneesDisponiblesCache.Count > 0
               && string.Equals(TourneesDisponiblesCacheCodeLivreur, codeLivreur, StringComparison.OrdinalIgnoreCase)
               && TourneesDisponiblesCacheDate?.Date == DateTime.Today;
    }

    /// <summary>
    /// Obtient un aperçu en lecture seule du cache des tournées disponibles.
    /// </summary>
    public IReadOnlyList<TourneeResumeDto> GetTourneesDisponiblesCache() => _tourneesDisponiblesCache;

    /// <summary>
    /// Enregistre dans le cache en mémoire la liste des tournées disponibles pour le livreur indiqué.
    /// La date est fixée sur DateTime.Today afin de permettre une invalidation automatique le lendemain.
    /// </summary>
    /// <param name="codeLivreur">Code du livreur.</param>
    /// <param name="tournees">Liste des tournées à stocker.</param>
    public void SaveTourneesDisponiblesCache(string codeLivreur, IEnumerable<TourneeResumeDto> tournees)
    {
        _tourneesDisponiblesCache.Clear();
        _tourneesDisponiblesCache.AddRange(tournees);
        TourneesDisponiblesCacheCodeLivreur = codeLivreur;
        TourneesDisponiblesCacheDate = DateTime.Today;
    }

    /// <summary>
    /// Vide le cache mémoire des tournées disponibles et réinitialise les métadonnées associées.
    /// </summary>
    public void ClearTourneesDisponiblesCache()
    {
        _tourneesDisponiblesCache.Clear();
        TourneesDisponiblesCacheCodeLivreur = null;
        TourneesDisponiblesCacheDate = null;
    }

    /// <summary>
    /// Supprime automatiquement le cache des appels API journaliers si la date enregistrée n'est pas égale à aujourd'hui.
    /// Doit être appelé avant d'utiliser le cache afin de garantir que les données ne sont pas réutilisées d'un jour sur l'autre.
    /// </summary>
    public void ClearDailyApiCacheIfNeeded()
    {
        if (TourneesDisponiblesCacheDate?.Date != DateTime.Today)
        {
            ClearTourneesDisponiblesCache();
        }
    }
}
