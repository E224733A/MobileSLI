using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;

namespace MobileSLI.ViewModels;

public sealed class ChoixTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly TourneesApiService _tourneesApiService;
    private readonly DatabaseService _databaseService;

    private string _searchText = string.Empty;
    private TourneeListItemViewModel? _selectedTournee;

    public ChoixTourneeViewModel(
        AppStateService appStateService,
        TourneesApiService tourneesApiService,
        DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _tourneesApiService = tourneesApiService;
        _databaseService = databaseService;

        Tournees = new ObservableCollection<TourneeListItemViewModel>();

        SelectTourneeCommand = new Command<TourneeListItemViewModel>(SelectTournee);

        ContinueCommand = new Command(
            async () => await ContinueAsync(),
            () => SelectedTournee is not null && !IsBusy);

        RefreshCommand = new Command(
            async () => await LoadTourneesAsync(forceReload: true),
            () => !IsBusy);

        BackCommand = new Command(
            async () => await Shell.Current.GoToAsync(".."));
    }

    public ObservableCollection<TourneeListItemViewModel> Tournees { get; }

    public string DateText => (_appStateService.DateTourneeAutorisee ?? DateTime.Today).ToString("dd/MM/yyyy");

    public string LivreurText =>
        _appStateService.CurrentLivreur?.NomLivreur ?? "Livreur non identifié";

    public string CountText => $"{Tournees.Count} tournée(s) disponible(s)";

    public string HelpText =>
        "Choisissez une tournée proposée pour aujourd'hui. Une seule tournée peut être sélectionnée à la fois.";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadTourneesAsync(forceReload: true);
            }
        }
    }

    public TourneeListItemViewModel? SelectedTournee
    {
        get => _selectedTournee;
        set
        {
            if (SetProperty(ref _selectedTournee, value))
            {
                foreach (var tournee in Tournees)
                {
                    tournee.IsSelected = ReferenceEquals(tournee, value);
                }

                RefreshCommandStates();
            }
        }
    }

    public ICommand SelectTourneeCommand { get; }

    public ICommand ContinueCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand BackCommand { get; }

    public void LoadTournees()
    {
        _ = LoadTourneesAsync(forceReload: false);
    }

    public async Task LoadTourneesAsync(bool forceReload = false)
    {
        if (IsBusy)
        {
            return;
        }

        if (_appStateService.CurrentLivreur is null)
        {
            ErrorMessage = "Aucun livreur sélectionné.";
            return;
        }

        try
        {
            IsBusy = true;
            RefreshCommandStates();

            ErrorMessage = string.Empty;
            SelectedTournee = null;
            Tournees.Clear();

            /*
             * Le mobile ne transmet plus dateTournee à l'API.
             * La date métier est calculée côté API et récupérée dans la réponse.
             */
            var tournees = await _tourneesApiService.GetTourneesDuJourAsync(
                _appStateService.CurrentLivreur.CodeLivreur);

            var apiDate = _tourneesApiService.LastDateTourneeApi
                          ?? _appStateService.DateTourneeAutorisee
                          ?? DateTime.Today;

            _appStateService.DateTourneeAutorisee = apiDate.Date;

            await _databaseService.ExpireOldActiveTourneesAsync(apiDate.Date);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                tournees = tournees
                    .Where(tournee =>
                        tournee.CodeTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                        || tournee.LibelleTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var tournee in tournees)
            {
                Tournees.Add(new TourneeListItemViewModel(tournee));
            }

            if (Tournees.Count == 0)
            {
                ErrorMessage = "Aucune tournée disponible pour aujourd'hui.";
            }

            OnPropertyChanged(nameof(DateText));
            OnPropertyChanged(nameof(CountText));
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible de charger les tournées du jour : {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private void SelectTournee(TourneeListItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedTournee = item;
        _appStateService.SelectedTournee = item.Dto;
        _appStateService.DateTourneeAutorisee = item.Dto.DateTournee.Date;
        ErrorMessage = string.Empty;
    }

    private async Task ContinueAsync()
    {
        if (SelectedTournee is null)
        {
            ErrorMessage = "Sélectionnez une tournée.";
            return;
        }

        _appStateService.SelectedTournee = SelectedTournee.Dto;
        _appStateService.DateTourneeAutorisee = SelectedTournee.Dto.DateTournee.Date;

        await Shell.Current.GoToAsync(nameof(ConfirmationTourneePage));
    }

    private void RefreshCommandStates()
    {
        if (ContinueCommand is Command continueCommand)
        {
            continueCommand.ChangeCanExecute();
        }

        if (RefreshCommand is Command refreshCommand)
        {
            refreshCommand.ChangeCanExecute();
        }
    }
}
