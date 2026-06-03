using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Services;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

public sealed class SyncResultViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly INavigationService _navigationService;

    public SyncResultViewModel(
        AppStateService appStateService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _navigationService = navigationService;

        BackHomeCommand = new Command(async () => await BackHomeAsync());
    }

    public string Message =>
        string.IsNullOrWhiteSpace(_appStateService.LastSyncResult?.Message)
            ? "Tournée envoyée avec succès."
            : _appStateService.LastSyncResult.Message;

    public string DateText =>
        (_appStateService.LastSyncResult?.DateResult ?? DateTime.Now)
        .ToString("dd/MM/yyyy à HH:mm");

    public ICommand BackHomeCommand { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(DateText));
    }

    private async Task BackHomeAsync()
    {
        await _navigationService.GoToAsync("//AccueilPage");
    }
}
