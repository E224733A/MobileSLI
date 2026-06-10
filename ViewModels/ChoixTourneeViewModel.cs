using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de la page de choix de tournée.
/// Cette page récupère les tournées disponibles pour le livreur courant,
/// garde un cache mémoire journalier et mémorise la date métier retournée par l'API.
/// </summary>
public sealed class ChoixTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly TourneesApiService _tourneesApiService;
    private readonly DatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    // Source mémoire des tournées chargées pour la journée. Elle est filtrée localement
    // dans ApplyTourneesFilter() pour éviter de rappeler l'API à chaque modification du texte de recherche.
    private readonly List<TourneeResumeDto> _tourneesSource = new();

    private string _searchText = string.Empty;
    private TourneeListItemViewModel? _selectedTournee;

    public ChoixTourneeViewModel(
        AppStateService appStateService,
        TourneesApiService tourneesApiService,
        DatabaseService databaseService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _tourneesApiService = tourneesApiService;
        _databaseService = databaseService;
        _navigationService = navigationService;

        Tournees = new ObservableCollection<TourneeListItemViewModel>();

        SelectTourneeCommand = new Command<TourneeListItemViewModel>(SelectTournee);

        ContinueCommand = new Command(
            async () => await ContinueAsync(),
            () => SelectedTournee is not null && !IsBusy);

        RefreshCommand = new Command(
            async () => await LoadTourneesAsync(forceReload: true),
            () => !IsBusy);

        BackCommand = new Command(
            async () => await _navigationService.GoBackAsync());
    }

    /// <summary>
    /// Collection de ViewModels représentant chaque résumé de tournée.
    /// </summary>
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
                // Filtre local uniquement : l'API n'est pas rappelée à chaque frappe utilisateur.
                ApplyTourneesFilter();
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

    /// <summary>
    /// Charge les tournées sans forcer de rechargement API.
    /// Méthode appelée indirectement par le code-behind au démarrage de l'écran.
    /// </summary>
    public void LoadTournees()
    {
        _ = LoadTourneesAsync(forceReload: false);
    }

    /// <summary>
    /// Charge la liste des tournées du jour.
    /// Utilise le cache mémoire si disponible, sinon appelle l'API,
    /// puis expire les tournées locales qui ne correspondent plus à la date métier courante.
    /// </summary>
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

            _appStateService.ClearDailyApiCacheIfNeeded();

            var codeLivreur = _appStateService.CurrentLivreur.CodeLivreur;
            List<TourneeResumeDto> tournees;

            if (!forceReload && _appStateService.HasTourneesDisponiblesCacheForToday(codeLivreur))
            {
                tournees = _appStateService.GetTourneesDisponiblesCache().ToList();
            }
            else
            {
                tournees = (await _tourneesApiService.GetTourneesDuJourAsync(codeLivreur)).ToList();
                _appStateService.SaveTourneesDisponiblesCache(codeLivreur, tournees);
            }

            var apiDate = _tourneesApiService.LastDateTourneeApi
                          ?? _appStateService.DateTourneeAutorisee
                          ?? DateTime.Today;

            _appStateService.DateTourneeAutorisee = apiDate.Date;

            // Sécurité locale : les tournées anciennes ne doivent plus rester envoyables.
            await _databaseService.ExpireOldActiveTourneesAsync(apiDate.Date);

            _tourneesSource.Clear();
            _tourneesSource.AddRange(tournees);

            ApplyTourneesFilter();

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

    /// <summary>
    /// Filtre la liste des tournées en mémoire selon le texte de recherche, et alimente la collection de ViewModels.
    /// </summary>
    private void ApplyTourneesFilter()
    {
        Tournees.Clear();

        IEnumerable<TourneeResumeDto> filtered = _tourneesSource;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(tournee =>
                tournee.CodeTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || tournee.LibelleTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var tournee in filtered)
        {
            Tournees.Add(new TourneeListItemViewModel(tournee, SelectTournee));
        }

        OnPropertyChanged(nameof(CountText));
    }

    /// <summary>
    /// Sélectionne une tournée et mémorise sa date métier dans l'état courant.
    /// </summary>
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

    /// <summary>
    /// Valide la sélection et passe à l'écran de confirmation.
    /// </summary>
    private async Task ContinueAsync()
    {
        if (SelectedTournee is null)
        {
            ErrorMessage = "Sélectionnez une tournée.";
            return;
        }

        _appStateService.SelectedTournee = SelectedTournee.Dto;
        _appStateService.DateTourneeAutorisee = SelectedTournee.Dto.DateTournee.Date;

        await _navigationService.GoToAsync(nameof(ConfirmationTourneePage));
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
