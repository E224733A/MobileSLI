using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class SyncResultViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;

    public SyncResultViewModel(AppStateService appStateService)
    {
        _appStateService = appStateService;
        BackHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//AccueilPage"));
    }

    public string Message => _appStateService.LastSyncResult?.Message ?? "Tournée envoyée avec succès";
    public string DateText => (_appStateService.LastSyncResult?.DateResult ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm");
    public string LignesText => $"{_appStateService.LastSyncResult?.LignesEnvoyees ?? 0} envoyées";

    public ICommand BackHomeCommand { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(LignesText));
    }
}
