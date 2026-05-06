using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class ConfirmationTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly ApiService _apiService;
    private readonly DatabaseService _databaseService;
    private readonly DemoDataService _demoDataService;

    private string _loadMessage = string.Empty;

    public ConfirmationTourneeViewModel(AppStateService appStateService, ApiService apiService, DatabaseService databaseService, DemoDataService demoDataService)
    {
        _appStateService = appStateService;
        _apiService = apiService;
        _databaseService = databaseService;
        _demoDataService = demoDataService;

        LoadTourneeCommand = new Command(async () => await LoadTourneeAsync(), () => !IsBusy);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public string LivreurText => _appStateService.CurrentLivreur?.NomLivreur ?? "Livreur non identifié";
    public string DateText => DateTime.Today.ToString("dd/MM/yyyy");
    public string TourneeText => _appStateService.SelectedTournee is null ? "Aucune tournée" : $"{_appStateService.SelectedTournee.CodeTournee} — {_appStateService.SelectedTournee.LibelleTournee}";
    public string NombrePointsText => _appStateService.SelectedTournee is null ? "" : $"{_appStateService.SelectedTournee.NombrePoints} points de livraison prévus";

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
            LoadMessage = "Chargement depuis l'API…";

            var dto = await _apiService.GetTourneeJourAsync(
                DateTime.Today,
                _appStateService.SelectedTournee.CodeTournee,
                _appStateService.CurrentLivreur.CodeLivreur);

            var tourneeId = await _databaseService.SaveTourneeAsync(dto);
            _appStateService.CurrentTourneeId = tourneeId;
            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        catch (Exception ex)
        {
            LoadMessage = "API indisponible. Chargement d'une tournée de démonstration pour continuer les tests.";
            var demo = _demoDataService.BuildTourneeJour(_appStateService.SelectedTournee, _appStateService.CurrentLivreur);
            var tourneeId = await _databaseService.SaveTourneeAsync(demo);
            _appStateService.CurrentTourneeId = tourneeId;
            await Shell.Current.DisplayAlert("Mode démonstration", $"La tournée de démonstration a été chargée localement. Détail technique : {ex.Message}", "OK");
            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        finally
        {
            IsBusy = false;
            ((Command)LoadTourneeCommand).ChangeCanExecute();
        }
    }
}
