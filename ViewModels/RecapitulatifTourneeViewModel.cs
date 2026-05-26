using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;

namespace MobileSLI.ViewModels;

public sealed class RecapitulatifTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly SynchronisationService _synchronisationService;
    private readonly HealthApiService _healthApiService;

    private LocalTournee? _tournee;
    private int _totalClients;
    private int _valides;
    private int _nonFaits;
    private int _anomalies;
    private string _commentaireGlobal = string.Empty;
    private string _connectionMessage = string.Empty;

    public RecapitulatifTourneeViewModel(
        AppStateService appStateService,
        DatabaseService databaseService,
        SynchronisationService synchronisationService,
        HealthApiService healthApiService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _synchronisationService = synchronisationService;
        _healthApiService = healthApiService;

        Articles = new ObservableCollection<RecapArticleViewModel>();

        TestConnectionCommand = new Command(
            async () => await TestConnectionAsync(showAlert: true),
            () => !IsBusy);

        SendCommand = new Command(
            async () => await SendAsync(),
            () => !IsBusy);

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public ObservableCollection<RecapArticleViewModel> Articles { get; }

    public string ResumeText => _tournee is null ? "Résumé" : $"{_tournee.CodeTournee} — {_tournee.LibelleTournee}";
    public string DateText => _tournee is null ? string.Empty : _tournee.DateTournee.ToString("dd/MM/yyyy");
    public string LivreurText => _tournee?.NomLivreur ?? string.Empty;

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
        LoadingMessage = "Préparation du récapitulatif...";

        _tournee = await _databaseService.GetTourneeAsync(_appStateService.CurrentTourneeId);
        if (_tournee is null)
        {
            ErrorMessage = "Aucune tournée locale trouvée.";
            return;
        }

        /*
         * Corrige les clients fermés avant le calcul du récapitulatif :
         * ils doivent être comptés comme traités, en NON_FAIT avec commentaire automatique.
         */
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
    }

    private async Task<bool> TestConnectionAsync(bool showAlert)
    {
        if (IsBusy)
        {
            return false;
        }

        try
        {
            LoadingMessage = "Test de la connexion au dépôt...";
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
            LoadingMessage = "Envoi de la tournée...";
            SetBusy(true);

            await _databaseService.UpdateCommentaireGlobalAsync(
                _appStateService.CurrentTourneeId,
                CommentaireGlobal);

            var result = await _synchronisationService.SynchroniserAsync(
                _appStateService.CurrentTourneeId);

            _appStateService.LastSyncResult = result;

            if (result.Success)
            {
                await Shell.Current.GoToAsync(nameof(SyncResultPage));
            }
            else
            {
                await Shell.Current.GoToAsync(nameof(SyncErrorPage));
            }
        }
        finally
        {
            SetBusy(false);
        }
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
