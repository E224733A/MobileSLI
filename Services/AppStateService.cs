using MobileSLI.Models;

namespace MobileSLI.Services;

public sealed class AppStateService
{
    public LivreurDto? CurrentLivreur { get; set; }
    public TourneeResumeDto? SelectedTournee { get; set; }
    public int CurrentTourneeId { get; set; }
    public int SelectedLigneId { get; set; }
    public OperationResult? LastSyncResult { get; set; }
}
