using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Navigation;
using System.Linq;

namespace MobileSLI.ViewModels;

/*
 * ViewModel de la page listant les points de livraison (lignes) pour une tournée.
 * Cette version ajoute uniquement le filtre métier "Commentaires exceptionnels".
 * Les commentaires sont déjà présents dans les lignes locales et déjà affichés sur les bandeaux clients.
 */
public sealed class ListePointsLivraisonViewModel : BaseViewModel
{
    private const string FiltreTous = "TOUS";
    private const string FiltreAFaire = "A_FAIRE";
    private const string FiltreFait = "FAIT";
    private const string FiltreFerme = "FERME";

    public const string FiltreCommentairesExceptionnels = "COMMENTAIRES_EXCEPTIONNELS";

    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    private string _currentFilter = FiltreTous;
    private LocalTournee? _tournee;
    private bool _hasClosedClients;
    private bool _hasCommentairesExceptionnels;

    public ListePointsLivraisonViewModel(
        AppStateService appStateService,
        DatabaseService databaseService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _navigationService = navigationService;

        Lignes = new ObservableCollection<LigneListItemViewModel>();

        OpenDetailCommand = new Command<LigneListItemViewModel>(async item => await OpenDetailAsync(item));
        SetFilterCommand = new Command<string>(async filter => await SetFilterAsync(filter));
        GoDechargementCommand = new Command(async () => await _navigationService.GoToAsync(nameof(DechargementPage)));
        GoRecapCommand = new Command(async () => await _navigationService.GoToAsync(nameof(RecapitulatifTourneePage)));
    }

    public ObservableCollection<LigneListItemViewModel> Lignes { get; }

    public string HeaderText => _tournee is null
        ? "Points de livraison"
        : $"{_tournee.CodeTournee} — {_tournee.LibelleTournee}";

    public string SubtitleText => _tournee is null
        ? "Suivre l'avancement de la tournée."
        : $"{_tournee.DateTournee:dd/MM/yyyy} · {_tournee.NomLivreur}";

    public string CurrentFilter
    {
        get => _currentFilter;
        set => SetProperty(ref _currentFilter, value);
    }

    public bool HasClosedClients
    {
        get => _hasClosedClients;
        private set => SetProperty(ref _hasClosedClients, value);
    }

    public bool HasCommentairesExceptionnels
    {
        get => _hasCommentairesExceptionnels;
        private set => SetProperty(ref _hasCommentairesExceptionnels, value);
    }

    public ICommand OpenDetailCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand GoDechargementCommand { get; }
    public ICommand GoRecapCommand { get; }

    public void ApplyNavigationFilter(string? filter)
    {
        if (string.Equals(filter, FiltreCommentairesExceptionnels, StringComparison.OrdinalIgnoreCase))
        {
            CurrentFilter = FiltreCommentairesExceptionnels;
        }
    }

    public async Task LoadAsync()
    {
        if (_appStateService.CurrentTourneeId <= 0)
        {
            var latest = await _databaseService.GetLatestTourneeAsync();
            if (latest is not null)
            {
                _appStateService.CurrentTourneeId = latest.Id;
            }
        }

        _tournee = await _databaseService.GetTourneeAsync(_appStateService.CurrentTourneeId);

        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(SubtitleText));

        /*
         * Sécurité métier : les clients fermés sont traités automatiquement
         * comme NON_FAIT avec le commentaire "Client fermé". Cela corrige aussi
         * les tournées déjà chargées avant l'ajout de cette règle.
         */
        await _databaseService.NormalizeClosedLinesAsync(_appStateService.CurrentTourneeId);

        var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);

        HasClosedClients = lignes.Any(ligne => ligne.EstFerme);
        HasCommentairesExceptionnels = lignes.Any(ligne =>
            !string.IsNullOrWhiteSpace(ligne.CommentaireExceptionnel));

        if (!HasClosedClients && CurrentFilter == FiltreFerme)
        {
            CurrentFilter = FiltreTous;
        }

        if (!HasCommentairesExceptionnels && CurrentFilter == FiltreCommentairesExceptionnels)
        {
            CurrentFilter = FiltreTous;
        }

        var filtered = CurrentFilter switch
        {
            FiltreAFaire => lignes.Where(ligne =>
                !ligne.EstFerme &&
                ligne.StatutPassage == StatutPassageConstants.AFaire),

            FiltreFait => lignes.Where(ligne =>
                !ligne.EstFerme &&
                ligne.StatutPassage == StatutPassageConstants.Fait),

            FiltreFerme => lignes.Where(ligne => ligne.EstFerme),

            FiltreCommentairesExceptionnels => lignes.Where(ligne =>
                !string.IsNullOrWhiteSpace(ligne.CommentaireExceptionnel)),

            _ => lignes
        };

        Lignes.Clear();

        foreach (var ligne in filtered.OrderBy(ligne => ligne.OrdreArret))
        {
            Lignes.Add(new LigneListItemViewModel(ligne, async item => await OpenDetailAsync(item)));
        }
    }

    private async Task SetFilterAsync(string? filter)
    {
        var requestedFilter = string.IsNullOrWhiteSpace(filter)
            ? FiltreTous
            : filter;

        if (requestedFilter == FiltreFerme && !HasClosedClients)
        {
            return;
        }

        if (requestedFilter == FiltreCommentairesExceptionnels && !HasCommentairesExceptionnels)
        {
            return;
        }

        CurrentFilter = requestedFilter;
        await LoadAsync();
    }

    private async Task OpenDetailAsync(LigneListItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _appStateService.SelectedLigneId = item.Id;
        await _navigationService.GoToAsync(nameof(DetailPointLivraisonPage));
    }
}
