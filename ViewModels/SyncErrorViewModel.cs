using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class SyncErrorViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;

    public SyncErrorViewModel(AppStateService appStateService)
    {
        _appStateService = appStateService;
        BackHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//AccueilPage"));
        BackRecapCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public string Message => _appStateService.LastSyncResult?.Message ?? "Erreur lors de l'envoi.";
    public string TechnicalDetail => string.IsNullOrWhiteSpace(_appStateService.LastSyncResult?.TechnicalDetail)
        ? "Aucun détail technique disponible."
        : _appStateService.LastSyncResult.TechnicalDetail;
    public string ActionText => _appStateService.LastSyncResult?.AlreadySynchronized == true
        ? "Ne pas renvoyer. Contacter le responsable logistique ou informatique si une correction est nécessaire."
        : "Vérifiez la connexion Wi-Fi du dépôt, puis réessayez depuis le récapitulatif.";

    public ICommand BackHomeCommand { get; }
    public ICommand BackRecapCommand { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(TechnicalDetail));
        OnPropertyChanged(nameof(ActionText));
    }
}
