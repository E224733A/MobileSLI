using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Services;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de l'écran de succès de synchronisation.
/// Il affiche le dernier résultat confirmé par l'API et ramène l'utilisateur à l'accueil.
/// </summary>
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

    /// <summary>
    /// Message de succès affiché. Un message par défaut est utilisé si l'API n'a rien renvoyé.
    /// </summary>
    public string Message =>
        string.IsNullOrWhiteSpace(_appStateService.LastSyncResult?.Message)
            ? "Tournée envoyée avec succès."
            : _appStateService.LastSyncResult.Message;

    /// <summary>
    /// Date locale du résultat de synchronisation.
    /// </summary>
    public string DateText =>
        (_appStateService.LastSyncResult?.DateResult ?? DateTime.Now)
        .ToString("dd/MM/yyyy à HH:mm");

    public ICommand BackHomeCommand { get; }

    /// <summary>
    /// Rafraîchit les informations affichées si le résultat de synchronisation a été mis à jour.
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(DateText));
    }

    /// <summary>
    /// Retourne à l'accueil après une synchronisation réussie.
    /// </summary>
    private async Task BackHomeAsync()
    {
        await _navigationService.GoToAsync("//AccueilPage");
    }
}
