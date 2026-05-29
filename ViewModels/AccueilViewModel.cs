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
    private string _diagnosticMessage = string.Empty;
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

        ExportDatabaseCommand = new Command(
            async () => await ExportDatabaseAsync(),
            () => !IsBusy && IsDiagnosticVisible);
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

    public string DiagnosticMessage
    {
        get => _diagnosticMessage;
        set
        {
            if (SetProperty(ref _diagnosticMessage, value))
            {
                OnPropertyChanged(nameof(HasDiagnosticMessage));
            }
        }
    }

    public bool HasDiagnosticMessage => !string.IsNullOrWhiteSpace(DiagnosticMessage);

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsDiagnosticVisible => true;

    public ICommand TestConnectionCommand { get; }

    public ICommand ContinueCommand { get; }

    public ICommand SaveApiUrlCommand { get; }

    public ICommand ExportDatabaseCommand { get; }

    public async Task CheckActiveTourneeOnStartupAsync()
    {
        if (_appStateService.HasCheckedActiveTourneeOnStartup || IsBusy)
        {
            return;
        }

        _appStateService.HasCheckedActiveTourneeOnStartup = true;

        try
        {
            LoadingMessage = "Vérification des données locales";
            SetBusy(true);

            var expiredCount = await _databaseService.ExpireOldActiveTourneesAsync();
            var deletedCount = await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
            var abandonedDeletedCount = await _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30);

            var activeTournee = await _databaseService.GetActiveTourneeAsync();

            if (activeTournee is null)
            {
                if (expiredCount > 0)
                {
                    ConnectionTitle = "Tournée expirée détectée";
                    ConnectionMessage =
                        $"{expiredCount} ancienne(s) tournée(s) non synchronisée(s) ont été verrouillée(s) en lecture seule. " +
                        "Rechargez les tournées du jour depuis l'API.";

                    return;
                }

                if (deletedCount > 0 || abandonedDeletedCount > 0)
                {
                    ConnectionTitle = "Nettoyage local effectué";
                    ConnectionMessage =
                        $"{deletedCount} ancienne(s) tournée(s) synchronisée(s) et " +
                        $"{abandonedDeletedCount} tournée(s) abandonnée(s) ont été supprimée(s) du téléphone.";
                }

                return;
            }

            ConnectionTitle = "Tournée locale détectée";
            ConnectionMessage =
                $"Une tournée non synchronisée est présente sur le téléphone : " +
                $"{activeTournee.CodeTournee} — {activeTournee.LibelleTournee} du {activeTournee.DateTournee:dd/MM/yyyy}.";

            var reprendre = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Reprendre votre tournée ?",
                "Une tournée du jour non synchronisée est déjà présente. Voulez-vous la reprendre ?",
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
            LoadingMessage = "Test de la connexion API";
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

    private async Task ExportDatabaseAsync()
    {
        if (IsBusy || !IsDiagnosticVisible)
        {
            return;
        }

        try
        {
            LoadingMessage = "Export de la base SQLite";
            SetBusy(true);
            ErrorMessage = string.Empty;
            DiagnosticMessage = string.Empty;

            var exportPath = await _databaseService.ExportDatabaseToDownloadsAsync();

            DiagnosticMessage = $"Base SQLite exportée : {exportPath}";

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Export terminé",
                        $"La base SQLite a été copiée dans :\n{exportPath}",
                        "OK");
                }
            });
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible d'exporter la base SQLite : {exception.Message}";
            DiagnosticMessage = ErrorMessage;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Export impossible",
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

    private async Task ContinueAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Préparation de l'identification";
            SetBusy(true);
            ErrorMessage = string.Empty;

            var expiredCount = await _databaseService.ExpireOldActiveTourneesAsync();
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
            await _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30);

            if (expiredCount > 0)
            {
                ConnectionTitle = "Tournée expirée détectée";
                ConnectionMessage =
                    $"{expiredCount} ancienne(s) tournée(s) non synchronisée(s) ont été verrouillée(s) en lecture seule. " +
                    "Rechargez les tournées du jour depuis l'API.";
            }

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

        if (ExportDatabaseCommand is Command exportCommand)
        {
            exportCommand.ChangeCanExecute();
        }
    }
}
