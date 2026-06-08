using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de la page d'accueil.
/// Cette version propose la reprise d'une tournée locale active via une carte et rend
/// l'outil d'export SQLite visible en Release comme en Debug. Elle met également en cache
/// certaines données pour améliorer la stabilité terrain.
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
    /// </summary>
    public async Task CheckActiveTourneeOnStartupAsync()
    {
        /*
         * La reprise automatique doit être proposée uniquement au démarrage
         * réel de l'application, par exemple après fermeture depuis les
         * applications récentes puis réouverture.
         *
         * Une tournée locale ancienne ne doit jamais être proposée à la reprise.
         * Elle est verrouillée localement avec le statut EXPIREE. Cela empêche
         * toute modification accidentelle sans ajouter de route API ni bloquer
         * les validations ligne par ligne pendant une tournée du jour.
         */
        if (_appStateService.HasCheckedActiveTourneeOnStartup || IsBusy)
        {
            // Même si la vérification a déjà eu lieu, charge la tournée locale active pour l'affichage de la carte.
            await LoadActiveTourneeLocaleAsync();
            return;
        }

        _appStateService.HasCheckedActiveTourneeOnStartup = true;

        try
        {
            LoadingMessage = "Vérification des données locales";
            SetBusy(true);
            ErrorMessage = string.Empty;

            // Vide le cache API journalier à l'ouverture afin de ne pas réutiliser des listes obsolètes
            _appStateService.ClearDailyApiCacheIfNeeded();

            // Charge la tournée locale pour l'affichage initial de la carte
            await LoadActiveTourneeLocaleAsync();

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
            if (reprendre)
            {
                _appStateService.CurrentTourneeId = activeTournee.Id;
                _appStateService.SelectedLigneId = 0;
                await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
                return;
            }
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
            // Recharge la tournée locale pour mettre à jour l'affichage de la carte après les interactions
            await LoadActiveTourneeLocaleAsync();
        }
    }

    /// <summary>
    /// Charge la tournée locale active en base et met à jour les propriétés de reprise.
    /// </summary>
    private async Task LoadActiveTourneeLocaleAsync()
    {
        _tourneeLocaleActive = await _databaseService.GetActiveTourneeAsync();
        OnPropertyChanged(nameof(HasTourneeLocaleActive));
        OnPropertyChanged(nameof(TourneeLocaleActiveText));

        if (ReprendreTourneeLocaleCommand is Command repriseCommand)
        {
            repriseCommand.ChangeCanExecute();
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

        _appStateService.CurrentTourneeId = _tourneeLocaleActive.Id;
        _appStateService.SelectedLigneId = 0;

        await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
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

            await _navigationService.GoToAsync(nameof(IdentificationLivreurPage));
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

        if (ReprendreTourneeLocaleCommand is Command repriseCommand)
        {
            repriseCommand.ChangeCanExecute();
        }
    }
}
