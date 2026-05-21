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
        (_appStateService.SelectedTournee?.DateTournee
         ?? _appStateService.DateTourneeAutorisee
         ?? DateTime.Today).ToString("dd/MM/yyyy");

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
            LoadingMessage = "Vérification des données locales...";
            SetBusy(true);

            ErrorMessage = string.Empty;
            LoadMessage = string.Empty;

            var selectedTournee = _appStateService.SelectedTournee;
            var dateTourneeAutorisee = selectedTournee.DateTournee.Date;
            _appStateService.DateTourneeAutorisee = dateTourneeAutorisee;

            /*
             * Une tournée d'une date antérieure à la date métier renvoyée par l'API
             * ne doit jamais être proposée à la reprise.
             * Elle est verrouillée localement en statut EXPIREE.
             */
            var expiredCount = await _databaseService.ExpireOldActiveTourneesAsync(dateTourneeAutorisee);
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);

            var activeTournee = await _databaseService.GetActiveTourneeAsync(dateTourneeAutorisee);
            if (activeTournee is not null)
            {
                var reprendre = await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Reprendre votre tournée ?",
                    $"Une tournée non synchronisée du jour est déjà présente : " +
                    $"{activeTournee.CodeTournee} du {activeTournee.DateTournee:dd/MM/yyyy}. Voulez-vous la reprendre ?",
                    "Reprendre",
                    "Retour");

                if (reprendre)
                {
                    _appStateService.CurrentTourneeId = activeTournee.Id;
                    _appStateService.SelectedLigneId = 0;

                    await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
                    return;
                }

                await Shell.Current.GoToAsync("..");
                return;
            }

            if (expiredCount > 0)
            {
                LoadMessage =
                    $"{expiredCount} ancienne(s) tournée(s) ont été marquée(s) EXPIREE et ne peuvent plus être reprises.";
            }

            LoadingMessage = "Chargement de la tournée depuis l'API...";
            LoadMessage = "Chargement depuis l'API…";

            var dto = await _tourneesApiService.GetTourneeJourAsync(
                selectedTournee.DateTournee,
                selectedTournee.CodeTournee,
                _appStateService.CurrentLivreur.CodeLivreur);

            if (dto.DateTournee != default)
            {
                _appStateService.DateTourneeAutorisee = dto.DateTournee.Date;
            }

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
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        if (LoadTourneeCommand is Command loadCommand)
        {
            loadCommand.ChangeCanExecute();
        }
    }
}
