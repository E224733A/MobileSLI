using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Diagnostics;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de la page d'accueil.
/// Cette version garde le comportement existant, mais sécurise le démarrage afin
/// qu'une erreur SQLite, purge locale ou reprise tournée ne ferme pas brutalement
/// l'application Android.
/// </summary>
public sealed class AccueilViewModel : BaseViewModel
{
    private readonly HealthApiService _healthApiService;
    private readonly SettingsService _settingsService;
    private readonly ConnectivityService _connectivityService;
    private readonly DatabaseService _databaseService;
    private readonly AppStateService _appStateService;
    private readonly INavigationService _navigationService;

    private string _apiBaseUrl;
    private string _connectionTitle = "Connexion non testée";
    private string _connectionMessage = "Testez la connexion au Wi-Fi du dépôt avant de continuer.";
    private string _diagnosticMessage = string.Empty;
    private bool _isConnected;

    private LocalTournee? _tourneeLocaleActive;

    public AccueilViewModel(
        HealthApiService healthApiService,
        SettingsService settingsService,
        ConnectivityService connectivityService,
        DatabaseService databaseService,
        AppStateService appStateService,
        INavigationService navigationService)
    {
        _healthApiService = healthApiService;
        _settingsService = settingsService;
        _connectivityService = connectivityService;
        _databaseService = databaseService;
        _appStateService = appStateService;
        _navigationService = navigationService;
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
            () => !IsBusy);

        ReprendreTourneeLocaleCommand = new Command(
            async () => await ReprendreTourneeLocaleAsync(),
            () => HasTourneeLocaleActive && !IsBusy);
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

    /// <summary>
    /// Indique si le diagnostic (export SQLite) doit être visible. Désormais toujours vrai en Debug et Release.
    /// </summary>
    public bool IsDiagnosticVisible => true;

    /// <summary>
    /// Indique si une tournée locale active existe et peut être reprise.
    /// </summary>
    public bool HasTourneeLocaleActive => _tourneeLocaleActive is not null;

    /// <summary>
    /// Texte descriptif de la tournée locale active à afficher dans la carte de reprise.
    /// </summary>
    public string TourneeLocaleActiveText => _tourneeLocaleActive is null
        ? string.Empty
        : string.IsNullOrWhiteSpace(_tourneeLocaleActive.LibelleTournee)
            ? $"{_tourneeLocaleActive.CodeTournee} du {_tourneeLocaleActive.DateTournee:dd/MM/yyyy}"
            : $"{_tourneeLocaleActive.CodeTournee} — {_tourneeLocaleActive.LibelleTournee} du {_tourneeLocaleActive.DateTournee:dd/MM/yyyy}";

    public ICommand TestConnectionCommand { get; }

    public ICommand ContinueCommand { get; }

    public ICommand SaveApiUrlCommand { get; }

    public ICommand ExportDatabaseCommand { get; }

    public ICommand ReprendreTourneeLocaleCommand { get; }

    /// <summary>
    /// Vérifie au démarrage si une tournée locale active existe et propose éventuellement une reprise.
    /// Cette méthode est volontairement défensive : elle ne doit jamais faire fermer l'application.
    /// </summary>
    public async Task CheckActiveTourneeOnStartupAsync()
    {
        if (_appStateService.HasCheckedActiveTourneeOnStartup || IsBusy)
        {
            await LoadActiveTourneeLocaleSafeAsync("startup déjà contrôlé");
            return;
        }

        _appStateService.HasCheckedActiveTourneeOnStartup = true;

        try
        {
            LoadingMessage = "Vérification des données locales";
            SetBusy(true);
            ErrorMessage = string.Empty;

            // Vide le cache API journalier à l'ouverture afin de ne pas réutiliser des listes obsolètes.
            _appStateService.ClearDailyApiCacheIfNeeded();

            // Charge la tournée locale pour l'affichage initial de la carte.
            await LoadActiveTourneeLocaleSafeAsync("chargement initial tournée active");

            var expiredCount = await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.ExpireOldActiveTourneesAsync(),
                "expiration anciennes tournées actives");

            var deletedCount = await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7),
                "purge anciennes tournées synchronisées");

            var abandonedDeletedCount = await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30),
                "purge tournées abandonnées");

            var activeTournee = await GetActiveTourneeSafeAsync("lecture tournée active après nettoyage");
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
            if (reprendre)
            {
                _appStateService.CurrentTourneeId = activeTournee.Id;
                _appStateService.SelectedLigneId = 0;
                await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
            }
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, "AccueilViewModel.CheckActiveTourneeOnStartupAsync");

            ErrorMessage = $"Impossible de vérifier la tournée locale : {exception.Message}";
            ConnectionTitle = "Vérification locale impossible";
            ConnectionMessage = ErrorMessage;
            DiagnosticMessage =
                $"Erreur démarrage capturée sans fermeture de l'application. Log local : {AppCrashLogger.LogPath}";
        }
        finally
        {
            SetBusy(false);
            await LoadActiveTourneeLocaleSafeAsync("rechargement final affichage accueil");
        }
    }

    private async Task<int> ExecuteDatabaseStartupStepAsync(Func<Task<int>> action, string context)
    {
        try
        {
            return await action();
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, $"AccueilViewModel startup database step - {context}");

            ErrorMessage = $"Diagnostic local : {exception.Message}";
            DiagnosticMessage =
                $"Une étape locale a échoué ({context}). L'application reste ouverte. Log : {AppCrashLogger.LogPath}";

            return 0;
        }
    }

    private async Task<LocalTournee?> GetActiveTourneeSafeAsync(string context)
    {
        try
        {
            return await _databaseService.GetActiveTourneeAsync();
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, $"AccueilViewModel.GetActiveTourneeSafeAsync - {context}");

            ErrorMessage = $"Lecture de la tournée locale impossible : {exception.Message}";
            DiagnosticMessage =
                $"Erreur SQLite locale capturée. Log : {AppCrashLogger.LogPath}";

            return null;
        }
    }

    /// <summary>
    /// Charge la tournée locale active en base et met à jour les propriétés de reprise.
    /// Cette version ne laisse pas remonter d'exception vers OnAppearing.
    /// </summary>
    private async Task LoadActiveTourneeLocaleSafeAsync(string context)
    {
        try
        {
            _tourneeLocaleActive = await _databaseService.GetActiveTourneeAsync();
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, $"AccueilViewModel.LoadActiveTourneeLocaleSafeAsync - {context}");

            _tourneeLocaleActive = null;
            ErrorMessage = $"Impossible de lire la tournée locale : {exception.Message}";
            ConnectionTitle = "Base locale indisponible";
            ConnectionMessage = ErrorMessage;
            DiagnosticMessage =
                $"Erreur SQLite locale capturée sans fermeture de l'application. Log : {AppCrashLogger.LogPath}";
        }
        finally
        {
            OnPropertyChanged(nameof(HasTourneeLocaleActive));
            OnPropertyChanged(nameof(TourneeLocaleActiveText));

            if (ReprendreTourneeLocaleCommand is Command repriseCommand)
            {
                repriseCommand.ChangeCanExecute();
            }
        }
    }

    /// <summary>
    /// Navigue directement vers la liste des points de livraison en reprenant la tournée locale active.
    /// </summary>
    private async Task ReprendreTourneeLocaleAsync()
    {
        if (_tourneeLocaleActive is null)
        {
            return;
        }

        try
        {
            _appStateService.CurrentTourneeId = _tourneeLocaleActive.Id;
            _appStateService.SelectedLigneId = 0;

            await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, "AccueilViewModel.ReprendreTourneeLocaleAsync");
            ErrorMessage = $"Impossible de reprendre la tournée : {exception.Message}";
            ConnectionTitle = "Reprise impossible";
            ConnectionMessage = ErrorMessage;
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
            AppCrashLogger.Log(exception, "AccueilViewModel.TestConnectionAsync");

            IsConnected = false;
            ConnectionTitle = "Erreur lors du test";
            ConnectionMessage = exception.Message;
            ErrorMessage = exception.Message;
            DiagnosticMessage = $"Erreur API capturée. Log : {AppCrashLogger.LogPath}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExportDatabaseAsync()
    {
        if (IsBusy)
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
            AppCrashLogger.Log(exception, "AccueilViewModel.ExportDatabaseAsync");

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

            /*
             * Le nettoyage reste autorisé ici pour éviter qu'une tournée d'hier
             * bloque le chargement du jour. On ne propose jamais de reprendre
             * une tournée expirée depuis le bouton Continuer.
             */
            var expiredCount = await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.ExpireOldActiveTourneesAsync(),
                "expiration anciennes tournées depuis Continuer");

            await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7),
                "purge anciennes synchronisées depuis Continuer");

            await ExecuteDatabaseStartupStepAsync(
                () => _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30),
                "purge abandonnées depuis Continuer");

            if (expiredCount > 0)
            {
                ConnectionTitle = "Tournée expirée détectée";
                ConnectionMessage =
                    $"{expiredCount} ancienne(s) tournée(s) non synchronisée(s) ont été verrouillée(s) en lecture seule. " +
                    "Rechargez les tournées du jour depuis l'API.";
            }

            await _navigationService.GoToAsync(nameof(IdentificationLivreurPage));
        }
        catch (Exception exception)
        {
            AppCrashLogger.Log(exception, "AccueilViewModel.ContinueAsync");

            ErrorMessage = $"Impossible d'ouvrir l'écran suivant : {exception.Message}";
            ConnectionTitle = "Navigation impossible";
            ConnectionMessage = ErrorMessage;
            DiagnosticMessage = $"Erreur navigation capturée. Log : {AppCrashLogger.LogPath}";

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

        if (ReprendreTourneeLocaleCommand is Command repriseCommand)
        {
            repriseCommand.ChangeCanExecute();
        }
    }
}
