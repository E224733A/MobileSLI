using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;

namespace MobileSLI.ViewModels;

public sealed class ConfirmationTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly TourneesApiService _tourneesApiService;
    private readonly DatabaseService _databaseService;
    private readonly DemoDataService _demoDataService;

    private string _loadMessage = string.Empty;

    public ConfirmationTourneeViewModel(
        AppStateService appStateService,
        TourneesApiService tourneesApiService,
        DatabaseService databaseService,
        DemoDataService demoDataService)
    {
        _appStateService = appStateService;
        _tourneesApiService = tourneesApiService;
        _databaseService = databaseService;
        _demoDataService = demoDataService;

        LoadTourneeCommand = new Command(
            async () => await LoadTourneeAsync(),
            () => !IsBusy);

        BackCommand = new Command(
            async () => await Shell.Current.GoToAsync(".."));
    }

    public string LivreurText =>
        _appStateService.CurrentLivreur?.NomLivreur ?? "Livreur non identifié";

    public string DateText =>
        DateTime.Today.ToString("dd/MM/yyyy");

    public string TourneeText =>
        _appStateService.SelectedTournee is null
            ? "Aucune tournée"
            : $"{_appStateService.SelectedTournee.CodeTournee} — {_appStateService.SelectedTournee.LibelleTournee}";

    public string NombrePointsText =>
        _appStateService.SelectedTournee is null
            ? string.Empty
            : $"{_appStateService.SelectedTournee.NombrePoints} points de livraison prévus";

    public string LoadMessage
    {
        get => _loadMessage;
        set => SetProperty(ref _loadMessage, value);
    }

    public ICommand LoadTourneeCommand { get; }

    public ICommand BackCommand { get; }

    private async Task LoadTourneeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_appStateService.CurrentLivreur is null || _appStateService.SelectedTournee is null)
        {
            ErrorMessage = "Livreur ou tournée manquant.";
            return;
        }

        try
        {
            IsBusy = true;
            RefreshCommandStates();
            ErrorMessage = string.Empty;
            LoadMessage = "Chargement depuis l'API…";

            var dto = await _tourneesApiService.GetTourneeJourAsync(
                DateTime.Today,
                _appStateService.SelectedTournee.CodeTournee,
                _appStateService.CurrentLivreur.CodeLivreur);

            var tourneeId = await _databaseService.SaveTourneeAsync(dto);

            _appStateService.CurrentTourneeId = tourneeId;
            _appStateService.SelectedLigneId = 0;

            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        catch (Exception exception)
        {
            /*
             * Mode de secours conservé pendant la migration.
             * Il permet de continuer à tester l'application si l'API ou le réseau est indisponible.
             * À désactiver en production.
             */
            LoadMessage = "API indisponible. Chargement d'une tournée de démonstration pour continuer les tests.";

            var demo = _demoDataService.BuildTourneeJour(
                _appStateService.SelectedTournee,
                _appStateService.CurrentLivreur);

            var tourneeId = await _databaseService.SaveTourneeAsync(demo);

            _appStateService.CurrentTourneeId = tourneeId;
            _appStateService.SelectedLigneId = 0;

            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Mode démonstration",
                $"La tournée de démonstration a été chargée localement. Détail technique : {exception.Message}",
                "OK");

            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        if (LoadTourneeCommand is Command loadCommand)
        {
            loadCommand.ChangeCanExecute();
        }
    }
}
