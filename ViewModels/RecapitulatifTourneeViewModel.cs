using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class RecapitulatifTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly SynchronisationService _synchronisationService;

    private LocalTournee? _tournee;
    private int _totalClients;
    private int _valides;
    private int _nonFaits;
    private int _anomalies;
    private string _commentaireGlobal = string.Empty;

    public RecapitulatifTourneeViewModel(AppStateService appStateService, DatabaseService databaseService, SynchronisationService synchronisationService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _synchronisationService = synchronisationService;

        Articles = new ObservableCollection<RecapArticleViewModel>();
        SendCommand = new Command(async () => await SendAsync(), () => !IsBusy);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public ObservableCollection<RecapArticleViewModel> Articles { get; }

    public string ResumeText => _tournee is null ? "Résumé" : $"{_tournee.CodeTournee} — {_tournee.LibelleTournee}";
    public string DateText => _tournee is null ? string.Empty : _tournee.DateTournee.ToString("dd/MM/yyyy");
    public string LivreurText => _tournee?.NomLivreur ?? string.Empty;
    public int TotalClients { get => _totalClients; set => SetProperty(ref _totalClients, value); }
    public int Valides { get => _valides; set => SetProperty(ref _valides, value); }
    public int NonFaits { get => _nonFaits; set => SetProperty(ref _nonFaits, value); }
    public int Anomalies { get => _anomalies; set => SetProperty(ref _anomalies, value); }

    public string CommentaireGlobal
    {
        get => _commentaireGlobal;
        set => SetProperty(ref _commentaireGlobal, value);
    }

    public ICommand SendCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadAsync()
    {
        _tournee = await _databaseService.GetTourneeAsync(_appStateService.CurrentTourneeId);
        if (_tournee is null)
        {
            ErrorMessage = "Aucune tournée locale trouvée.";
            return;
        }

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

    private async Task SendAsync()
    {
        if (IsBusy)
        {
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
            IsBusy = true;
            await _databaseService.UpdateCommentaireGlobalAsync(_appStateService.CurrentTourneeId, CommentaireGlobal);
            var result = await _synchronisationService.SynchroniserAsync(_appStateService.CurrentTourneeId);
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
            IsBusy = false;
            ((Command)SendCommand).ChangeCanExecute();
        }
    }
}
