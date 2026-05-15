using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;

namespace MobileSLI.ViewModels;

public sealed class AccueilViewModel : BaseViewModel
{
    private readonly HealthApiService _healthApiService;
    private readonly SettingsService _settingsService;
    private readonly ConnectivityService _connectivityService;
    private readonly DatabaseService _databaseService;
    private readonly AppStateService _appStateService;

    private string _apiBaseUrl;
    private string _connectionTitle = "Connexion non testée";
    private string _connectionMessage = "Testez la connexion au Wi-Fi du dépôt avant de continuer.";
    private bool _isConnected;

    public AccueilViewModel(
        HealthApiService healthApiService,
        SettingsService settingsService,
        ConnectivityService connectivityService,
        DatabaseService databaseService,
        AppStateService appStateService)
    {
        _healthApiService = healthApiService;
        _settingsService = settingsService;
        _connectivityService = connectivityService;
        _databaseService = databaseService;
        _appStateService = appStateService;
        _apiBaseUrl = _settingsService.ApiBaseUrl;

        TestConnectionCommand = new Command(
            async () => await TestConnectionAsync(),
            () => !IsBusy);

        ContinueCommand = new Command(
            async () => await ContinueAsync(),
            () => !IsBusy);

        SaveApiUrlCommand = new Command(SaveApiUrl);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string ConnectionTitle
    {
        get => _connectionTitle;
        set => SetProperty(ref _connectionTitle, value);
    }

    public string ConnectionMessage
    {
        get => _connectionMessage;
        set => SetProperty(ref _connectionMessage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public ICommand TestConnectionCommand { get; }

    public ICommand ContinueCommand { get; }

    public ICommand SaveApiUrlCommand { get; }

    public async Task CheckActiveTourneeOnStartupAsync()
    {
        /*
         * La reprise automatique doit être proposée uniquement au démarrage
         * réel de l'application, par exemple après fermeture depuis les
         * applications récentes puis réouverture.
         *
         * Elle ne doit pas bloquer le bouton "Continuer vers l'identification".
         * Le flag est porté par AppStateService, singleton pendant la session,
         * pour éviter de réafficher la popup si AccueilPage ou son ViewModel
         * sont recréés pendant que l'application reste ouverte.
         */
        if (_appStateService.HasCheckedActiveTourneeOnStartup || IsBusy)
        {
            return;
        }

        _appStateService.HasCheckedActiveTourneeOnStartup = true;

        try
        {
            LoadingMessage = "Vérification des données locales...";
            SetBusy(true);

            var deletedCount = await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);

            var activeTournee = await _databaseService.GetActiveTourneeAsync();

            if (activeTournee is null)
            {
                if (deletedCount > 0)
                {
                    ConnectionTitle = "Nettoyage local effectué";
                    ConnectionMessage = $"{deletedCount} ancienne(s) tournée(s) synchronisée(s) ont été supprimée(s) du téléphone.";
                }

                return;
            }

            ConnectionTitle = "Tournée locale détectée";
            ConnectionMessage =
                $"Une tournée non synchronisée est présente sur le téléphone : " +
                $"{activeTournee.CodeTournee} — {activeTournee.LibelleTournee} du {activeTournee.DateTournee:dd/MM/yyyy}.";

            var reprendre = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Reprendre votre tournée ?",
                "Une tournée non synchronisée est déjà présente. Voulez-vous la reprendre ?",
                "Reprendre",
                "Retour");

            if (!reprendre)
            {
                return;
            }

            _appStateService.CurrentTourneeId = activeTournee.Id;

            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible de vérifier la tournée locale : {exception.Message}";
            ConnectionTitle = "Vérification locale impossible";
            ConnectionMessage = ErrorMessage;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SaveApiUrl()
    {
        _settingsService.ApiBaseUrl = ApiBaseUrl;
        ApiBaseUrl = _settingsService.ApiBaseUrl;

        ConnectionTitle = "Adresse API enregistrée";
        ConnectionMessage = _settingsService.ApiBaseUrl;
        ErrorMessage = string.Empty;
    }

    private async Task TestConnectionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Test de la connexion API...";
            SetBusy(true);
            ErrorMessage = string.Empty;

            SaveApiUrl();

            if (!_connectivityService.HasInternetOrLocalNetwork)
            {
                IsConnected = false;
                ConnectionTitle = "Aucun réseau";
                ConnectionMessage = "Le téléphone ne détecte pas de réseau. Vérifiez le Wi-Fi du dépôt.";
                return;
            }

            var result = await _healthApiService.TestConnectionAsync();

            IsConnected = result.Success;
            ConnectionTitle = result.Success ? "Connecté" : "Connexion impossible";
            ConnectionMessage = result.Success
                ? "Le téléphone peut contacter l'API."
                : result.Message;
        }
        catch (Exception exception)
        {
            IsConnected = false;
            ConnectionTitle = "Erreur lors du test";
            ConnectionMessage = exception.Message;
            ErrorMessage = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ContinueAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Préparation de l'identification...";
            SetBusy(true);
            ErrorMessage = string.Empty;

            /*
             * Le nettoyage reste autorisé ici, car il ne supprime que les
             * tournées déjà synchronisées/verrouillées et anciennes.
             *
             * En revanche, on ne propose plus la reprise d'une tournée active
             * depuis le bouton "Continuer vers l'identification".
             * La reprise est uniquement proposée au démarrage de l'application.
             */
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);

            await Shell.Current.GoToAsync(nameof(IdentificationLivreurPage));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible d'ouvrir l'écran suivant : {exception.Message}";
            ConnectionTitle = "Navigation impossible";
            ConnectionMessage = ErrorMessage;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Navigation impossible",
                        ErrorMessage,
                        "OK");
                }
            });
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;

        if (TestConnectionCommand is Command testCommand)
        {
            testCommand.ChangeCanExecute();
        }

        if (ContinueCommand is Command continueCommand)
        {
            continueCommand.ChangeCanExecute();
        }
    }
}
