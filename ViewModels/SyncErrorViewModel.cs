using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de l'écran d'erreur de synchronisation.
/// Il distingue les erreurs corrigeables, les erreurs de validation, les refus de date
/// et les cas déjà synchronisés pour éviter les renvois dangereux.
/// </summary>
public sealed class SyncErrorViewModel : BaseViewModel
{
    private const string DateTourneeNonAutoriseeCode = "DATE_TOURNEE_NON_AUTORISEE";
    private const string DateTourneeExpireeCode = "DATE_TOURNEE_EXPIREE";
    private const string TourneeLocaleExpireeCode = "TOURNEE_LOCALE_EXPIREE";
    private const string ValidationErrorCode = "VALIDATION_ERROR";

    private readonly AppStateService _appStateService;
    private readonly SynchronisationService _synchronisationService;
    private readonly HealthApiService _healthApiService;
    private readonly INavigationService _navigationService;

    public SyncErrorViewModel(
        AppStateService appStateService,
        SynchronisationService synchronisationService,
        HealthApiService healthApiService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _synchronisationService = synchronisationService;
        _healthApiService = healthApiService;
        _navigationService = navigationService;

        RetryCommand = new Command(
            async () => await RetryAsync(),
            () => !IsBusy && CanRetry);

        BackHomeCommand = new Command(async () => await _navigationService.GoToAsync("//AccueilPage"));
        BackRecapCommand = new Command(async () => await _navigationService.GoBackAsync());
    }

    public string PageTitle => IsValidationError()
        ? "Tournée incomplète"
        : "Erreur envoi";

    public string Subtitle => IsValidationError()
        ? "La tournée n'est pas encore prête à être envoyée."
        : "La tournée n'a pas pu être envoyée.";

    public string Message => _appStateService.LastSyncResult?.Message ?? "Erreur lors de l'envoi.";

    public string TechnicalDetail => string.IsNullOrWhiteSpace(_appStateService.LastSyncResult?.TechnicalDetail)
        ? "Aucun détail technique disponible."
        : _appStateService.LastSyncResult.TechnicalDetail;

    public string ActionText
    {
        get
        {
            if (IsValidationError())
            {
                return "Retournez au récapitulatif ou à la liste des clients, puis corrigez le point indiqué avant d’envoyer la tournée.";
            }

            if (IsDateTourneeNonAutorisee() || IsTourneeLocaleExpiree())
            {
                return "Ne pas renvoyer cette tournée. Rechargez les tournées du jour depuis le dépôt.";
            }

            if (_appStateService.LastSyncResult?.AlreadySynchronized == true)
            {
                return "Ne pas renvoyer. Contacter le responsable logistique ou informatique si une correction est nécessaire.";
            }

            return "Vérifiez la connexion Wi-Fi du dépôt, puis réessayez.";
        }
    }

    /// <summary>
    /// Autorise une nouvelle tentative uniquement pour les erreurs potentiellement temporaires.
    /// Les validations, dates expirées et déjà synchronisés sont volontairement non rejouables.
    /// </summary>
    public bool CanRetry => _appStateService.LastSyncResult?.AlreadySynchronized != true
                            && !IsValidationError()
                            && !IsDateTourneeNonAutorisee()
                            && !IsTourneeLocaleExpiree();

    public ICommand RetryCommand { get; }

    public ICommand BackHomeCommand { get; }

    public ICommand BackRecapCommand { get; }

    /// <summary>
    /// Rafraîchit l'affichage après modification du dernier résultat de synchronisation.
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(TechnicalDetail));
        OnPropertyChanged(nameof(ActionText));
        OnPropertyChanged(nameof(CanRetry));

        if (RetryCommand is Command retryCommand)
        {
            retryCommand.ChangeCanExecute();
        }
    }

    /// <summary>
    /// Retente l'envoi après vérification de l'accès API.
    /// La tentative n'est pas lancée si l'erreur précédente est fonctionnellement bloquante.
    /// </summary>
    private async Task RetryAsync()
    {
        if (IsBusy || !CanRetry)
        {
            return;
        }

        try
        {
            LoadingMessage = "Test de la connexion au dépôt";
            SetBusy(true);
            ErrorMessage = string.Empty;

            var health = await _healthApiService.TestConnectionAsync();

            if (!health.Success)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync(
                    "Connexion au dépôt requise",
                    "Impossible de joindre l’API. Veuillez vous connecter au Wi-Fi du dépôt avant de renvoyer la tournée.",
                    "OK");

                return;
            }

            LoadingMessage = "Nouvel envoi de la tournée";

            var result = await _synchronisationService.SynchroniserAsync(
                _appStateService.CurrentTourneeId);

            _appStateService.LastSyncResult = result;

            if (result.Success)
            {
                await _navigationService.GoToAsync(nameof(SyncResultPage));
            }
            else
            {
                Refresh();
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool IsValidationError()
    {
        var result = _appStateService.LastSyncResult;
        if (result is null)
        {
            return false;
        }

        return string.Equals(result.Code, ValidationErrorCode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Détecte un refus de date métier retourné par l'API.
    /// Ce cas ne doit pas être rejoué : il faut recharger les tournées du jour.
    /// </summary>
    private bool IsDateTourneeNonAutorisee()
    {
        var result = _appStateService.LastSyncResult;
        if (result is null)
        {
            return false;
        }

        return string.Equals(result.Code, DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(result.Code, DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase)
               || ContainsDateTourneeNonAutorisee(result.Message)
               || ContainsDateTourneeNonAutorisee(result.TechnicalDetail);
    }

    /// <summary>
    /// Détecte une tournée expirée localement.
    /// Ce cas indique que le payload local ne correspond plus à la journée autorisée.
    /// </summary>
    private bool IsTourneeLocaleExpiree()
    {
        var result = _appStateService.LastSyncResult;
        if (result is null)
        {
            return false;
        }

        return string.Equals(result.Code, TourneeLocaleExpireeCode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(result.Code, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase)
               || ContainsTourneeLocaleExpiree(result.Message)
               || ContainsTourneeLocaleExpiree(result.TechnicalDetail);
    }

    private static bool ContainsTourneeLocaleExpiree(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && (value.Contains(TourneeLocaleExpireeCode, StringComparison.OrdinalIgnoreCase)
                   || value.Contains("tournée est expirée", StringComparison.OrdinalIgnoreCase)
                   || value.Contains("tournee est expiree", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsDateTourneeNonAutorisee(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && (value.Contains(DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
                   || value.Contains(DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase));
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;

        if (RetryCommand is Command retryCommand)
        {
            retryCommand.ChangeCanExecute();
        }
    }
}
