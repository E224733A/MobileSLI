using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/*
 * ViewModel de la page de confirmation de tournée.
 * Cette version conserve le chargement existant et ajoute uniquement une information terrain :
 * après sauvegarde locale d'une tournée reçue depuis l'API, une fenêtre est affichée si au moins
 * un client possède un commentaire exceptionnel. Le livreur peut alors ouvrir directement la liste
 * filtrée sur les clients concernés.
 */
public sealed class ConfirmationTourneeViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly TourneesApiService _tourneesApiService;
    private readonly DatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    private LocalTournee? _activeTourneeEnConflit;
    private TourneeResumeDto? _selectedTourneeEnConflit;
    private bool _hasExistingActiveTournee;
    private string _existingActiveTourneeText = string.Empty;
    private string _selectedTourneeConflitText = string.Empty;
    private string _loadMessage = string.Empty;

    public ConfirmationTourneeViewModel(
        AppStateService appStateService,
        TourneesApiService tourneesApiService,
        DatabaseService databaseService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _tourneesApiService = tourneesApiService;
        _databaseService = databaseService;
        _navigationService = navigationService;

        LoadTourneeCommand = new Command(
            async () => await LoadTourneeAsync(),
            () => !IsBusy);

        BackCommand = new Command(
            async () => await _navigationService.GoBackAsync());

        ReprendreTourneeExistanteCommand = new Command(
            async () => await ReprendreTourneeExistanteAsync(),
            () => !IsBusy && _activeTourneeEnConflit is not null);

        RetourListeTourneesCommand = new Command(
            async () => await _navigationService.GoBackAsync(),
            () => !IsBusy);

        AbandonnerEtChargerTourneeCommand = new Command(
            async () => await AbandonnerEtChargerTourneeAsync(),
            () => !IsBusy && _activeTourneeEnConflit is not null && _selectedTourneeEnConflit is not null);
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

    /// <summary>
    /// Texte indiquant le nombre de points/clients pour la tournée sélectionnée.
    /// Renvoie une chaîne vide si l'information n'est pas disponible ou nulle ou égale à 0.
    /// </summary>
    public string NombrePointsText
    {
        get
        {
            var nombrePoints = _appStateService.SelectedTournee?.NombrePoints ?? 0;
            if (nombrePoints > 0)
            {
                return nombrePoints == 1
                    ? "1 point"
                    : $"{nombrePoints} points";
            }

            return string.Empty;
        }
    }

    public bool HasExistingActiveTournee
    {
        get => _hasExistingActiveTournee;
        set
        {
            if (SetProperty(ref _hasExistingActiveTournee, value))
            {
                OnPropertyChanged(nameof(CanLoadSelectedTournee));
            }
        }
    }

    public bool CanLoadSelectedTournee => !HasExistingActiveTournee;

    public string ExistingActiveTourneeText
    {
        get => _existingActiveTourneeText;
        set => SetProperty(ref _existingActiveTourneeText, value);
    }

    public string SelectedTourneeConflitText
    {
        get => _selectedTourneeConflitText;
        set => SetProperty(ref _selectedTourneeConflitText, value);
    }

    public string LoadMessage
    {
        get => _loadMessage;
        set => SetProperty(ref _loadMessage, value);
    }

    public ICommand LoadTourneeCommand { get; }

    public ICommand BackCommand { get; }

    public ICommand ReprendreTourneeExistanteCommand { get; }

    public ICommand RetourListeTourneesCommand { get; }

    public ICommand AbandonnerEtChargerTourneeCommand { get; }

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
            ClearExistingActiveTourneeConflict();

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
                if (IsSameTournee(activeTournee, selectedTournee))
                {
                    _appStateService.CurrentTourneeId = activeTournee.Id;
                    _appStateService.SelectedLigneId = 0;
                    LoadMessage = "Tournée déjà chargée localement. Ouverture de la reprise.";
                    await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
                    return;
                }

                ShowExistingActiveTourneeConflict(activeTournee, selectedTournee);
                return;
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

    private void ShowExistingActiveTourneeConflict(
        LocalTournee activeTournee,
        TourneeResumeDto selectedTournee)
    {
        _activeTourneeEnConflit = activeTournee;
        _selectedTourneeEnConflit = selectedTournee;

        ExistingActiveTourneeText = string.IsNullOrWhiteSpace(activeTournee.LibelleTournee)
            ? $"{activeTournee.CodeTournee} du {activeTournee.DateTournee:dd/MM/yyyy}"
            : $"{activeTournee.CodeTournee} — {activeTournee.LibelleTournee} du {activeTournee.DateTournee:dd/MM/yyyy}";

        SelectedTourneeConflitText = string.IsNullOrWhiteSpace(selectedTournee.LibelleTournee)
            ? $"{selectedTournee.CodeTournee} du {selectedTournee.DateTournee:dd/MM/yyyy}"
            : $"{selectedTournee.CodeTournee} — {selectedTournee.LibelleTournee} du {selectedTournee.DateTournee:dd/MM/yyyy}";

        HasExistingActiveTournee = true;

        LoadMessage = "Une tournée locale non synchronisée existe déjà sur ce téléphone.";
        RefreshCommandStates();
    }

    private async Task ReprendreTourneeExistanteAsync()
    {
        if (_activeTourneeEnConflit is null)
        {
            return;
        }

        _appStateService.CurrentTourneeId = _activeTourneeEnConflit.Id;
        _appStateService.SelectedLigneId = 0;

        await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
    }

    private async Task AbandonnerEtChargerTourneeAsync()
    {
        if (_activeTourneeEnConflit is null || _selectedTourneeEnConflit is null)
        {
            return;
        }

        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Abandonner la tournée locale ?",
            $"La tournée locale {_activeTourneeEnConflit.CodeTournee} ne sera plus proposée à la reprise sur ce téléphone.\n\n" +
            $"Nouvelle tournée à charger : {_selectedTourneeEnConflit.CodeTournee}.\n\n" +
            "Cette action doit être utilisée uniquement si la tournée locale est bloquée ou chargée par erreur.",
            "Abandonner et charger",
            "Annuler");

        if (!confirmed)
        {
            return;
        }

        try
        {
            LoadingMessage = "Chargement de la nouvelle tournée...";
            SetBusy(true);
            ErrorMessage = string.Empty;

            var activeTournee = _activeTourneeEnConflit;
            var selectedTournee = _selectedTourneeEnConflit;

            await _databaseService.AbandonnerTourneeLocaleAsync(
                activeTournee.Id,
                $"Remplacée par la tournée {selectedTournee.CodeTournee} depuis l'écran de confirmation.");

            await _databaseService.PurgeOldAbandonedTourneesAsync(retentionDays: 30);

            HasExistingActiveTournee = false;
            LoadMessage = $"La tournée locale {activeTournee.CodeTournee} a été abandonnée. Chargement de {selectedTournee.CodeTournee}.";

            ClearExistingActiveTourneeConflict();

            await LoadSelectedTourneeFromApiAsync(selectedTournee);
        }
        catch (Exception exception)
        {
            ErrorMessage =
                "Chargement impossible après abandon de la tournée locale. " +
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

        var lignes = await _databaseService.GetLignesAsync(tourneeId);
        var nombreCommentairesExceptionnels = lignes.Count(ligne =>
            !string.IsNullOrWhiteSpace(ligne.CommentaireExceptionnel));

        if (nombreCommentairesExceptionnels > 0)
        {
            var commentaireText = nombreCommentairesExceptionnels == 1
                ? "1 client a un commentaire exceptionnel."
                : $"{nombreCommentairesExceptionnels} clients ont un commentaire exceptionnel.";

            var voirCommentaires = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Commentaires exceptionnels",
                $"{commentaireText}\n\nVoulez-vous afficher les clients concernés ?",
                "Voir les clients concernés",
                "Voir plus tard");

            if (voirCommentaires)
            {
                await _navigationService.GoToAsync(
                    $"{nameof(ListePointsLivraisonPage)}?filtre={ListePointsLivraisonViewModel.FiltreCommentairesExceptionnels}");
                return;
            }

            LoadMessage = commentaireText;
        }

        await _navigationService.GoToAsync(nameof(ListePointsLivraisonPage));
    }

    private static bool IsSameTournee(LocalTournee activeTournee, TourneeResumeDto selectedTournee)
    {
        return string.Equals(activeTournee.CodeTournee, selectedTournee.CodeTournee, StringComparison.OrdinalIgnoreCase)
            && activeTournee.DateTournee.Date == selectedTournee.DateTournee.Date;
    }

    private void ClearExistingActiveTourneeConflict()
    {
        _activeTourneeEnConflit = null;
        _selectedTourneeEnConflit = null;
        ExistingActiveTourneeText = string.Empty;
        SelectedTourneeConflitText = string.Empty;
        HasExistingActiveTournee = false;
        RefreshCommandStates();
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

        if (ReprendreTourneeExistanteCommand is Command reprendreCommand)
        {
            reprendreCommand.ChangeCanExecute();
        }

        if (RetourListeTourneesCommand is Command retourCommand)
        {
            retourCommand.ChangeCanExecute();
        }

        if (AbandonnerEtChargerTourneeCommand is Command abandonnerCommand)
        {
            abandonnerCommand.ChangeCanExecute();
        }
    }
}
