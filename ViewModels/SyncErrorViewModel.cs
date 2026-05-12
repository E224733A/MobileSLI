using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;

namespace MobileSLI.ViewModels;

public sealed class SyncErrorViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly SynchronisationService _synchronisationService;
    private readonly HealthApiService _healthApiService;

    public SyncErrorViewModel(
        AppStateService appStateService,
        SynchronisationService synchronisationService,
        HealthApiService healthApiService)
    {
        _appStateService = appStateService;
        _synchronisationService = synchronisationService;
        _healthApiService = healthApiService;

        RetryCommand = new Command(
            async () => await RetryAsync(),
            () => !IsBusy && CanRetry);

        BackHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//AccueilPage"));
        BackRecapCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public string Message => _appStateService.LastSyncResult?.Message ?? "Erreur lors de l'envoi.";

    public string TechnicalDetail => string.IsNullOrWhiteSpace(_appStateService.LastSyncResult?.TechnicalDetail)
        ? "Aucun détail technique disponible."
        : _appStateService.LastSyncResult.TechnicalDetail;

    public string ActionText => _appStateService.LastSyncResult?.AlreadySynchronized == true
        ? "Ne pas renvoyer. Contacter le responsable logistique ou informatique si une correction est nécessaire."
        : "Vérifiez la connexion Wi-Fi du dépôt, puis réessayez.";

    public bool CanRetry => _appStateService.LastSyncResult?.AlreadySynchronized != true;

    public ICommand RetryCommand { get; }

    public ICommand BackHomeCommand { get; }

    public ICommand BackRecapCommand { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(TechnicalDetail));
        OnPropertyChanged(nameof(ActionText));
        OnPropertyChanged(nameof(CanRetry));

        if (RetryCommand is Command retryCommand)
        {
            retryCommand.ChangeCanExecute();
        }
    }

    private async Task RetryAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Test de la connexion au dépôt...";
            SetBusy(true);
            ErrorMessage = string.Empty;

            var health = await _healthApiService.TestConnectionAsync();

            if (!health.Success)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Connexion au dépôt requise",
                    "Impossible de joindre l’API. Veuillez vous connecter au Wi-Fi du dépôt avant de renvoyer la tournée.",
                    "OK");

                return;
            }

            LoadingMessage = "Nouvel envoi de la tournée...";

            var result = await _synchronisationService.SynchroniserAsync(
                _appStateService.CurrentTourneeId);

            _appStateService.LastSyncResult = result;

            if (result.Success)
            {
                await Shell.Current.GoToAsync(nameof(SyncResultPage));
            }
            else
            {
                Refresh();
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;

        if (RetryCommand is Command retryCommand)
        {
            retryCommand.ChangeCanExecute();
        }
    }
}
