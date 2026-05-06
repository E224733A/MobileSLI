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

    private string _loadMessage = string.Empty;

    public ConfirmationTourneeViewModel(
        AppStateService appStateService,
        TourneesApiService tourneesApiService,
        DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _tourneesApiService = tourneesApiService;
        _databaseService = databaseService;

        LoadTourneeCommand = new Command(
            async () => await LoadTourneeAsync(),
            () => !IsBusy);

        BackCommand = new Command(
            async () => await Shell.Current.GoToAsync(".."));
    }

    public string LivreurText =>
        _appStateService.CurrentLivreur?.NomLivreur ?? "Livreur non identifié";

    public string DateText =>
        (_appStateService.SelectedTournee?.DateTournee ?? DateTime.Today).ToString("dd/MM/yyyy");

    public string TourneeText =>
        _appStateService.SelectedTournee is null
            ? "Aucune tournée"
            : _appStateService.SelectedTournee.NomAffiche;

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

            var selectedTournee = _appStateService.SelectedTournee;

            var dto = await _tourneesApiService.GetTourneeJourAsync(
                selectedTournee.DateTournee,
                selectedTournee.CodeTournee,
                _appStateService.CurrentLivreur.CodeLivreur);

            var tourneeId = await _databaseService.SaveTourneeAsync(dto);

            _appStateService.CurrentTourneeId = tourneeId;
            _appStateService.SelectedLigneId = 0;

            LoadMessage = "Tournée chargée localement.";

            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
        }
        catch (Exception exception)
        {
            ErrorMessage =
                "Chargement impossible. La tournée sélectionnée n'a pas pu être récupérée depuis l'API. " +
                $"Détail : {exception.Message}";

            LoadMessage = ErrorMessage;

            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Chargement impossible",
                ErrorMessage,
                "OK");
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
