using MobileSLI.Models;

namespace MobileSLI.Services;

public sealed class AppStateService
{
    public LivreurDto? CurrentLivreur { get; set; }
    public TourneeResumeDto? SelectedTournee { get; set; }
    public int CurrentTourneeId { get; set; }
    public int SelectedLigneId { get; set; }
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
}
