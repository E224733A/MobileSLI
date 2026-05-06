using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class ListePointsLivraisonViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;

    private string _currentFilter = "TOUS";
    private LocalTournee? _tournee;

    public ListePointsLivraisonViewModel(AppStateService appStateService, DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;

        Lignes = new ObservableCollection<LigneListItemViewModel>();
        OpenDetailCommand = new Command<LigneListItemViewModel>(async item => await OpenDetailAsync(item));
        SetFilterCommand = new Command<string>(async filter => await SetFilterAsync(filter));
        GoDechargementCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(DechargementPage)));
        GoRecapCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(RecapitulatifTourneePage)));
    }

    public ObservableCollection<LigneListItemViewModel> Lignes { get; }

    public string HeaderText => _tournee is null ? "Points de livraison" : $"{_tournee.CodeTournee} — {_tournee.LibelleTournee}";
    public string SubtitleText => _tournee is null ? "Suivre l'avancement de la tournée." : $"{_tournee.DateTournee:dd/MM/yyyy} · {_tournee.NomLivreur}";
    public string CurrentFilter
    {
        get => _currentFilter;
        set => SetProperty(ref _currentFilter, value);
    }

    public ICommand OpenDetailCommand { get; }
    public ICommand SetFilterCommand { get; }
    public ICommand GoDechargementCommand { get; }
    public ICommand GoRecapCommand { get; }

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

        var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);
        var filtered = CurrentFilter switch
        {
            "A_FAIRE" => lignes.Where(l => l.StatutPassage == StatutPassageConstants.AFaire),
            "FAIT" => lignes.Where(l => l.StatutPassage == StatutPassageConstants.Fait),
            "NON_FAIT" => lignes.Where(l => l.StatutPassage == StatutPassageConstants.NonFait),
            "ANOMALIE" => lignes.Where(l => l.StatutPassage == StatutPassageConstants.Anomalie),
            _ => lignes
        };

        Lignes.Clear();
        foreach (var ligne in filtered)
        {
            Lignes.Add(new LigneListItemViewModel(ligne));
        }
    }

    private async Task SetFilterAsync(string? filter)
    {
        CurrentFilter = string.IsNullOrWhiteSpace(filter) ? "TOUS" : filter;
        await LoadAsync();
    }

    private async Task OpenDetailAsync(LigneListItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _appStateService.SelectedLigneId = item.Id;
        await Shell.Current.GoToAsync(nameof(DetailPointLivraisonPage));
    }
}
