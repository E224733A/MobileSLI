using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

public sealed class RecapitulatifTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly SynchronisationService _synchronisationService;
    private readonly HealthApiService _healthApiService;
    private readonly INavigationService _navigationService;

    private LocalTournee? _tournee;
    private int _totalClients;
    private int _valides;
    private int _nonFaits;
    private int _anomalies;
    private string _commentaireGlobal = string.Empty;
    private string _connectionMessage = string.Empty;
    private string _kilometrageArriveeText = string.Empty;

    public RecapitulatifTourneeViewModel(
        AppStateService appStateService,
        DatabaseService databaseService,
        SynchronisationService synchronisationService,
        HealthApiService healthApiService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _synchronisationService = synchronisationService;
        _healthApiService = healthApiService;
        _navigationService = navigationService;

        Articles = new ObservableCollection<RecapArticleViewModel>();

        TestConnectionCommand = new Command(
            async () => await TestConnectionAsync(showAlert: true),
            () => !IsBusy);

        SendCommand = new Command(
            async () => await SendAsync(),
            () => !IsBusy);

        BackCommand = new Command(async () => await _navigationService.GoBackAsync());
    }

    public ObservableCollection<RecapArticleViewModel> Articles { get; }

    public string ResumeText => _tournee is null ? "Résumé" : $"{_tournee.CodeTournee} — {_tournee.LibelleTournee}";
    public string DateText => _tournee is null ? string.Empty : _tournee.DateTournee.ToString("dd/MM/yyyy");
    public string LivreurText => _tournee?.NomLivreur ?? string.Empty;

    public string CamionText
    {
        get
        {
            var camion = _appStateService.CurrentCamion;
            if (camion is null)
            {
                return "Camion : non renseigné";
            }

            var immatriculation = camion.Immatriculation?.Trim() ?? string.Empty;
            var libelle = camion.LibelleCamion?.Trim() ?? string.Empty;
            var code = camion.CodeCamion?.Trim() ?? string.Empty;

            var identifiant = !string.IsNullOrWhiteSpace(immatriculation)
                ? immatriculation
                : code;

            if (!string.IsNullOrWhiteSpace(identifiant)
                && !string.IsNullOrWhiteSpace(libelle)
                && !string.Equals(identifiant, libelle, StringComparison.OrdinalIgnoreCase))
            {
                return $"Camion : {identifiant} - {libelle}";
            }

            return string.IsNullOrWhiteSpace(identifiant)
                ? "Camion : non renseigné"
                : $"Camion : {identifiant}";
        }
    }

    public string KilometrageDepartText => _appStateService.KilometrageDepart.HasValue
        ? $"Départ : {_appStateService.KilometrageDepart.Value} km"
        : "Départ : non renseigné";

    public string DateDepartMobileText => _appStateService.DateDepartMobile.HasValue
        ? $"Départ mobile : {_appStateService.DateDepartMobile.Value:dd/MM/yyyy HH:mm}"
        : "Départ mobile : non renseigné";

    public bool HasDateDepartMobile => _appStateService.DateDepartMobile.HasValue;

    public bool HasTrajetDepartIncomplet =>
        _appStateService.CurrentCamion is null
        || !_appStateService.KilometrageDepart.HasValue
        || !_appStateService.DateDepartMobile.HasValue;

    public string TrajetDepartErreurText => HasTrajetDepartIncomplet
        ? "Camion ou kilométrage départ manquant. Revenez au choix camion avant d’envoyer la tournée."
        : string.Empty;

    public int TotalClients
    {
        get => _totalClients;
        set => SetProperty(ref _totalClients, value);
    }

    public int Valides
    {
        get => _valides;
        set => SetProperty(ref _valides, value);
    }

    public int NonFaits
    {
        get => _nonFaits;
        set => SetProperty(ref _nonFaits, value);
    }

    public int Anomalies
    {
        get => _anomalies;
        set => SetProperty(ref _anomalies, value);
    }

    public string CommentaireGlobal
    {
        get => _commentaireGlobal;
        set => SetProperty(ref _commentaireGlobal, value);
    }

    public string KilometrageArriveeText
    {
        get => _kilometrageArriveeText;
        set => SetProperty(ref _kilometrageArriveeText, value);
    }

    public string ConnectionMessage
    {
        get => _connectionMessage;
        set
        {
            if (SetProperty(ref _connectionMessage, value))
            {
                OnPropertyChanged(nameof(HasConnectionMessage));
            }
        }
    }

    public bool HasConnectionMessage => !string.IsNullOrWhiteSpace(ConnectionMessage);

    public ICommand TestConnectionCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        LoadingMessage = "Préparation du récapitulatif en cours";

        _tournee = await _databaseService.GetTourneeAsync(_appStateService.CurrentTourneeId);
        if (_tournee is null)
        {
            ErrorMessage = "Aucune tournée locale trouvée.";
            return;
        }

        await _databaseService.RestaurerTrajetDansAppStateAsync(_tournee.Id, _appStateService);

        if (_appStateService.KilometrageArrivee.HasValue && string.IsNullOrWhiteSpace(KilometrageArriveeText))
        {
            KilometrageArriveeText = _appStateService.KilometrageArrivee.Value.ToString(CultureInfo.InvariantCulture);
        }

        await _databaseService.NormalizeClosedLinesAsync(_tournee.Id);

        var lignes = await _databaseService.GetLignesAsync(_tournee.Id);
        TotalClients = lignes.Count;
        Valides = lignes.Count(l => l.StatutPassage != StatutPassageConstants.AFaire);
        NonFaits = lignes.Count(l => l.StatutPassage == StatutPassageConstants.NonFait);
        Anomalies = lignes.Count(l => l.StatutPassage == StatutPassageConstants.Anomalie);
        CommentaireGlobal = _tournee.CommentaireGlobal ?? string.Empty;

        var totals = new Dictionary<string, RecapArticleViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in lignes)
        {
            var quantites = await _databaseService.GetQuantitesAsync(ligne.Id);
            foreach (var quantite in quantites)
            {
                if (!totals.TryGetValue(quantite.CodeArticle, out var item))
                {
                    item = new RecapArticleViewModel { Libelle = quantite.Libelle };
                    totals.Add(quantite.CodeArticle, item);
                }

                item.TotalLivre += quantite.QuantiteLivree;
                item.TotalRecupere += quantite.QuantiteRecuperee;
            }
        }

        Articles.Clear();
        foreach (var item in totals.Values.OrderBy(i => i.Libelle))
        {
            Articles.Add(item);
        }

        OnPropertyChanged(nameof(ResumeText));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(LivreurText));
        OnPropertyChanged(nameof(CamionText));
        OnPropertyChanged(nameof(KilometrageDepartText));
        OnPropertyChanged(nameof(DateDepartMobileText));
        OnPropertyChanged(nameof(HasDateDepartMobile));
        OnPropertyChanged(nameof(HasTrajetDepartIncomplet));
        OnPropertyChanged(nameof(TrajetDepartErreurText));
    }

    private async Task<bool> TestConnectionAsync(bool showAlert)
    {
        if (IsBusy)
        {
            return false;
        }

        try
        {
            LoadingMessage = "Test de la connexion au dépôt en cours";
            SetBusy(true);
            ErrorMessage = string.Empty;

            var result = await _healthApiService.TestConnectionAsync();

            if (result.Success)
            {
                ConnectionMessage = "Connexion au dépôt OK. L’API est accessible.";

                if (showAlert)
                {
                    await Shell.Current.CurrentPage.DisplayAlertAsync(
                        "Connexion dépôt",
                        "Connexion au dépôt OK. L’API est accessible.",
                        "OK");
                }

                return true;
            }

            ConnectionMessage = string.Empty;
            ErrorMessage = "Connexion au dépôt impossible. Veuillez vous connecter au Wi-Fi du dépôt avant d’envoyer la tournée.";

            if (showAlert)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Connexion au dépôt requise",
                    "Impossible de joindre l’API. Veuillez vous connecter au Wi-Fi du dépôt avant d’envoyer la tournée.",
                    "OK");
            }

            return false;
        }
        catch
        {
            ConnectionMessage = string.Empty;
            ErrorMessage = "Connexion au dépôt impossible. Veuillez vous connecter au Wi-Fi du dépôt avant d’envoyer la tournée.";

            if (showAlert)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Connexion au dépôt requise",
                    "Impossible de joindre l’API. Veuillez vous connecter au Wi-Fi du dépôt avant d’envoyer la tournée.",
                    "OK");
            }

            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SendAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var validationMessage = await GetLocalBlockingValidationMessageAsync();
        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Tournée incomplète",
                validationMessage,
                "OK");

            return;
        }

        if (!TryValidateKilometrageArrivee(out var kilometrageArrivee, out var trajetValidationMessage))
        {
            ErrorMessage = trajetValidationMessage;

            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Trajet incomplet",
                trajetValidationMessage,
                "OK");

            return;
        }

        var apiDisponible = await TestConnectionAsync(showAlert: false);

        if (!apiDisponible)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Connexion au dépôt requise",
                "Impossible de joindre l’API. Veuillez vous connecter au Wi-Fi du dépôt avant d’envoyer la tournée. Les données restent enregistrées sur le téléphone.",
                "OK");

            return;
        }

        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Validation définitive",
            "Après synchronisation réussie, la tournée sera verrouillée et ne sera plus modifiable.",
            "Envoyer",
            "Annuler");

        if (!confirmed)
        {
            return;
        }

        try
        {
            LoadingMessage = "Envoi de la tournée en cours";
            SetBusy(true);

            _appStateService.KilometrageArrivee = kilometrageArrivee;
            _appStateService.DateArriveeMobile = DateTime.Now;

            await _databaseService.PersistTrajetArriveeAsync(
                _appStateService.CurrentTourneeId,
                kilometrageArrivee,
                _appStateService.DateArriveeMobile.Value);

            await _databaseService.UpdateCommentaireGlobalAsync(
                _appStateService.CurrentTourneeId,
                CommentaireGlobal);

            var result = await _synchronisationService.SynchroniserAsync(
                _appStateService.CurrentTourneeId);

            _appStateService.LastSyncResult = result;

            if (result.Success)
            {
                await _navigationService.GoToAsync(nameof(SyncResultPage));
            }
            else
            {
                await _navigationService.GoToAsync(nameof(SyncErrorPage));
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool TryValidateKilometrageArrivee(out int kilometrageArrivee, out string validationMessage)
    {
        kilometrageArrivee = 0;
        validationMessage = string.Empty;

        if (_appStateService.CurrentCamion is null)
        {
            validationMessage = "Camion manquant. Revenez au choix camion avant d’envoyer la tournée.";
            return false;
        }

        if (!_appStateService.KilometrageDepart.HasValue)
        {
            validationMessage = "Kilométrage départ manquant. Revenez au choix camion avant d’envoyer la tournée.";
            return false;
        }

        if (!_appStateService.DateDepartMobile.HasValue)
        {
            validationMessage = "Date départ mobile manquante. Revenez au choix camion avant d’envoyer la tournée.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(KilometrageArriveeText))
        {
            validationMessage = "Le kilométrage arrivée est obligatoire avant l’envoi.";
            return false;
        }

        if (!int.TryParse(
                KilometrageArriveeText.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out kilometrageArrivee))
        {
            validationMessage = "Le kilométrage arrivée doit être un nombre entier.";
            return false;
        }

        if (kilometrageArrivee < 0)
        {
            validationMessage = "Le kilométrage arrivée ne peut pas être négatif.";
            return false;
        }

        if (kilometrageArrivee < _appStateService.KilometrageDepart.Value)
        {
            validationMessage = "Le kilométrage arrivée doit être supérieur ou égal au kilométrage départ.";
            return false;
        }

        return true;
    }

    private async Task<string?> GetLocalBlockingValidationMessageAsync()
    {
        if (_appStateService.CurrentTourneeId <= 0)
        {
            return "Aucune tournée locale n'est sélectionnée pour l'envoi.";
        }

        await _databaseService.NormalizeClosedLinesAsync(_appStateService.CurrentTourneeId);

        var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);
        var ligneAFaire = lignes.FirstOrDefault(ligne =>
            string.Equals(ligne.StatutPassage, StatutPassageConstants.AFaire, StringComparison.OrdinalIgnoreCase));

        if (ligneAFaire is not null)
        {
            return $"Le point {ligneAFaire.NumClient} - {ligneAFaire.NomClient} est encore à faire. Validez tous les points avant d’envoyer la tournée.";
        }

        var ligneSansCommentaire = lignes.FirstOrDefault(ligne =>
            (string.Equals(ligne.StatutPassage, StatutPassageConstants.NonFait, StringComparison.OrdinalIgnoreCase)
             || string.Equals(ligne.StatutPassage, StatutPassageConstants.Anomalie, StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrWhiteSpace(ligne.CommentaireLivreur));

        if (ligneSansCommentaire is not null)
        {
            return $"Le point {ligneSansCommentaire.NumClient} - {ligneSansCommentaire.NomClient} nécessite un commentaire pour le statut choisi.";
        }

        return null;
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;

        if (TestConnectionCommand is Command testCommand)
        {
            testCommand.ChangeCanExecute();
        }

        if (SendCommand is Command sendCommand)
        {
            sendCommand.ChangeCanExecute();
        }
    }
}
