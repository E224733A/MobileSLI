using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class ListePointsLivraisonViewModel : BaseViewModel
{
    private const string FiltreTous = "TOUS";
    private const string FiltreAFaire = "A_FAIRE";
    private const string FiltreFait = "FAIT";
    private const string FiltreFerme = "FERME";

    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;

    private string _currentFilter = FiltreTous;
    private LocalTournee? _tournee;
    private bool _hasClosedClients;

    public ListePointsLivraisonViewModel(AppStateService appStateService, DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;

        LoadingMessage = "Chargement des points de livraison...";
        Lignes = new ObservableCollection<LigneListItemViewModel>();

        OpenDetailCommand = new Command<LigneListItemViewModel>(async item => await OpenDetailAsync(item), _ => !IsBusy);
        SetFilterCommand = new Command<string>(async filter => await SetFilterAsync(filter), _ => !IsBusy);
        GoDechargementCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(DechargementPage)), () => !IsBusy);
        GoRecapCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(RecapitulatifTourneePage)), () => !IsBusy);
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

    public ICommand OpenDetailCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand GoDechargementCommand { get; }
    public ICommand GoRecapCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Chargement des points de livraison...";
            SetBusy(true);
            await Task.Yield();

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

            await _databaseService.NormalizeClosedLinesAsync(_appStateService.CurrentTourneeId);

            var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);

            HasClosedClients = lignes.Any(ligne => ligne.EstFerme);

            if (!HasClosedClients && CurrentFilter == FiltreFerme)
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

                _ => lignes
            };

            Lignes.Clear();

            foreach (var ligne in filtered.OrderBy(ligne => ligne.OrdreArret))
            {
                Lignes.Add(new LigneListItemViewModel(ligne));
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SetFilterAsync(string? filter)
    {
        if (IsBusy)
        {
            return;
        }

        var requestedFilter = string.IsNullOrWhiteSpace(filter)
            ? FiltreTous
            : filter;

        if (requestedFilter == FiltreFerme && !HasClosedClients)
        {
            return;
        }

        CurrentFilter = requestedFilter;
        await LoadAsync();
    }

    private async Task OpenDetailAsync(LigneListItemViewModel? item)
    {
        if (IsBusy || item is null)
        {
            return;
        }

        _appStateService.SelectedLigneId = item.Id;
        await Shell.Current.GoToAsync(nameof(DetailPointLivraisonPage));
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        if (OpenDetailCommand is Command<LigneListItemViewModel> openCommand)
        {
            openCommand.ChangeCanExecute();
        }

        if (SetFilterCommand is Command<string> filterCommand)
        {
            filterCommand.ChangeCanExecute();
        }

        if (GoDechargementCommand is Command dechargementCommand)
        {
            dechargementCommand.ChangeCanExecute();
        }

        if (GoRecapCommand is Command recapCommand)
        {
            recapCommand.ChangeCanExecute();
        }
    }
}
