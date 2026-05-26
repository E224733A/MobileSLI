using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Models;
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
            await _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30);

            var activeTournee = await _databaseService.GetActiveTourneeAsync(dateTourneeAutorisee);
            if (activeTournee is not null)
            {
                var canContinue = await HandleExistingActiveTourneeAsync(activeTournee, selectedTournee);
                if (!canContinue)
                {
                    return;
                }
            }

            if (expiredCount > 0)
            {
                LoadMessage =
                    $"{expiredCount} ancienne(s) tournée(s) ont été marquée(s) EXPIREE et ne peuvent plus être reprises.";
            }

            await LoadSelectedTourneeFromApiAsync(selectedTournee);
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

    private async Task<bool> HandleExistingActiveTourneeAsync(
        LocalTournee activeTournee,
        TourneeResumeDto selectedTournee)
    {
        var selectedLabel = string.IsNullOrWhiteSpace(selectedTournee.LibelleTournee)
            ? selectedTournee.CodeTournee
            : $"{selectedTournee.CodeTournee} — {selectedTournee.LibelleTournee}";

        var activeLabel = string.IsNullOrWhiteSpace(activeTournee.LibelleTournee)
            ? activeTournee.CodeTournee
            : $"{activeTournee.CodeTournee} — {activeTournee.LibelleTournee}";

        var action = await Shell.Current.CurrentPage.DisplayActionSheetAsync(
            $"Une tournée non synchronisée existe déjà :\n{activeLabel} du {activeTournee.DateTournee:dd/MM/yyyy}\n\nQue voulez-vous faire ?",
            "Retour à la liste",
            $"Abandonner {activeTournee.CodeTournee} et charger {selectedTournee.CodeTournee}",
            "Reprendre cette tournée");

        if (string.Equals(action, "Reprendre cette tournée", StringComparison.OrdinalIgnoreCase))
        {
            _appStateService.CurrentTourneeId = activeTournee.Id;
            _appStateService.SelectedLigneId = 0;

            await Shell.Current.GoToAsync(nameof(ListePointsLivraisonPage));
            return false;
        }

        if (string.Equals(
                action,
                $"Abandonner {activeTournee.CodeTournee} et charger {selectedTournee.CodeTournee}",
                StringComparison.OrdinalIgnoreCase))
        {
            var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Abandonner la tournée locale ?",
                $"Attention : cette action abandonnera la tournée locale non synchronisée {activeLabel} du {activeTournee.DateTournee:dd/MM/yyyy}.\n\n" +
                $"Elle ne sera plus proposée à la reprise sur ce téléphone. Utilisez cette option uniquement si la tournée est bloquée ou chargée par erreur.\n\n" +
                $"Voulez-vous vraiment l'abandonner et charger {selectedLabel} ?",
                "Abandonner et charger",
                "Annuler");

            if (!confirmed)
            {
                return false;
            }

            await _databaseService.AbandonnerTourneeLocaleAsync(
                activeTournee.Id,
                $"Remplacée par la tournée {selectedTournee.CodeTournee} depuis l'écran de chargement.");

            await _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30);

            LoadMessage = $"La tournée locale {activeTournee.CodeTournee} a été abandonnée. Chargement de {selectedTournee.CodeTournee}.";
            return true;
        }

        await Shell.Current.GoToAsync("..");
        return false;
    }

    private async Task LoadSelectedTourneeFromApiAsync(TourneeResumeDto selectedTournee)
    {
        LoadingMessage = "Chargement de la tournée depuis l'API...";
        LoadMessage = "Chargement depuis l'API…";

        /*
         * Le mobile ne transmet plus dateTournee à l'API.
         * L'API calcule la date métier et renvoie la tournée complète.
         */
        var dto = await _tourneesApiService.GetTourneeJourAsync(
            selectedTournee.CodeTournee,
            _appStateService.CurrentLivreur!.CodeLivreur);

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
