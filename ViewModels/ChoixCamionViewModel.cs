using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de l'écran de choix camion.
/// Il charge les camions actifs depuis l'API, valide le kilométrage de départ
/// et alimente AppStateService avant le passage au choix de tournée.
/// </summary>
public sealed class ChoixCamionViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly CamionsApiService _camionsApiService;
    private readonly INavigationService _navigationService;

    private bool _hasLoaded;
    private CamionItemViewModel? _selectedCamion;
    private string _kilometrageDepartText = string.Empty;

    public ChoixCamionViewModel(
        AppStateService appStateService,
        CamionsApiService camionsApiService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _camionsApiService = camionsApiService;
        _navigationService = navigationService;

        LoadingMessage = "Chargement des camions...";
        Camions = new ObservableCollection<CamionItemViewModel>();

        // Reprise possible : si un kilométrage départ existe déjà dans l'état courant, on le réaffiche.
        _kilometrageDepartText = _appStateService.KilometrageDepart?.ToString(CultureInfo.InvariantCulture)
            ?? string.Empty;

        LoadCamionsCommand = new Command(
            async () => await LoadCamionsAsync(forceReload: true),
            () => !IsBusy);

        ContinueCommand = new Command(
            async () => await ContinueAsync(),
            () => !IsBusy);
    }

    public ObservableCollection<CamionItemViewModel> Camions { get; }

    public string LivreurText
    {
        get
        {
            var livreur = _appStateService.CurrentLivreur;

            if (livreur is null)
            {
                return "Livreur non sélectionné";
            }

            return string.IsNullOrWhiteSpace(livreur.NomLivreur)
                ? $"Livreur : {livreur.CodeLivreur}"
                : $"Livreur : {livreur.CodeLivreur} - {livreur.NomLivreur}";
        }
    }

    public string HelpText =>
        "Choisissez le camion utilisé pour la tournée, puis saisissez le kilométrage départ.";

    public string CountText => $"{Camions.Count} camion(s) disponible(s)";

    public CamionItemViewModel? SelectedCamion
    {
        get => _selectedCamion;
        set
        {
            if (SetProperty(ref _selectedCamion, value))
            {
                foreach (var camion in Camions)
                {
                    camion.IsSelected = ReferenceEquals(camion, value);
                }

                OnPropertyChanged(nameof(HasSelectedCamion));
                OnPropertyChanged(nameof(SelectedCamionText));
                RefreshCommandStates();
            }
        }
    }

    public bool HasSelectedCamion => SelectedCamion is not null;

    public string SelectedCamionText => SelectedCamion is null
        ? string.Empty
        : $"Camion sélectionné : {SelectedCamion.NomAffiche}";

    public string KilometrageDepartText
    {
        get => _kilometrageDepartText;
        set
        {
            if (SetProperty(ref _kilometrageDepartText, value))
            {
                ErrorMessage = string.Empty;
                OnPropertyChanged(nameof(HasKilometrageDepart));
                OnPropertyChanged(nameof(KilometrageDepartResumeText));
                RefreshCommandStates();
            }
        }
    }

    public bool HasKilometrageDepart => !string.IsNullOrWhiteSpace(KilometrageDepartText);

    public string KilometrageDepartResumeText => HasKilometrageDepart
        ? $"Kilométrage saisi : {KilometrageDepartText.Trim()}"
        : string.Empty;

    public ICommand LoadCamionsCommand { get; }

    public ICommand ContinueCommand { get; }

    /// <summary>
    /// Charge les camions disponibles depuis l'API.
    /// Le chargement est évité si la liste a déjà été chargée, sauf demande explicite de rechargement.
    /// </summary>
    public async Task LoadCamionsAsync(bool forceReload = false)
    {
        if (IsBusy)
        {
            return;
        }

        if (_hasLoaded && !forceReload)
        {
            RestoreCurrentCamionSelection();
            return;
        }

        ErrorMessage = string.Empty;
        IsBusy = true;
        RefreshCommandStates();

        try
        {
            Camions.Clear();
            SelectedCamion = null;

            var camions = await _camionsApiService.GetCamionsDisponiblesAsync();

            foreach (var camion in camions)
            {
                Camions.Add(new CamionItemViewModel(camion, SelectCamion));
            }

            _hasLoaded = true;

            if (Camions.Count == 0)
            {
                ErrorMessage = "Aucun camion actif disponible.";
            }
            else
            {
                RestoreCurrentCamionSelection();
            }

            OnPropertyChanged(nameof(CountText));
        }
        catch (ApiClientException exception)
        {
            ErrorMessage =
                $"Impossible de charger les camions depuis l'API. " +
                $"Route : {exception.Route}. " +
                $"Code HTTP : {exception.StatusCode}.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible de charger les camions : {exception.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// Sélectionne un camion dans la liste et met à jour les états visuels associés.
    /// </summary>
    private void SelectCamion(CamionItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedCamion = item;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Valide le choix camion et le kilométrage départ avant de continuer.
    /// Le camion, le kilométrage et l'heure de départ sont stockés en mémoire dans AppStateService ;
    /// ils seront ensuite persistés côté SQLite par le flux de chargement de tournée.
    /// </summary>
    private async Task ContinueAsync()
    {
        ErrorMessage = string.Empty;

        if (_appStateService.CurrentLivreur is null)
        {
            await RejectAsync("Aucun livreur sélectionné. Revenez à l'identification.");
            return;
        }

        if (SelectedCamion is null)
        {
            await RejectAsync("Sélectionnez un camion pour continuer.");
            return;
        }

        if (!TryValidateKilometrageDepart(out var kilometrageDepart, out var errorMessage))
        {
            await RejectAsync(errorMessage);
            return;
        }

        _appStateService.CurrentCamion = SelectedCamion.Dto;
        _appStateService.KilometrageDepart = kilometrageDepart;
        _appStateService.DateDepartMobile = DateTime.Now;

        await _navigationService.GoToAsync(nameof(ChoixTourneePage));
    }

    /// <summary>
    /// Affiche une erreur bloquante à l'utilisateur sans changer d'écran.
    /// </summary>
    private async Task RejectAsync(string message)
    {
        ErrorMessage = message;

        if (Shell.Current?.CurrentPage is not null)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Action requise",
                message,
                "OK");
        }
    }

    /// <summary>
    /// Valide le kilométrage départ saisi.
    /// Règle métier : valeur obligatoire, entière et positive ou égale à zéro.
    /// </summary>
    private bool TryValidateKilometrageDepart(out int kilometrageDepart, out string errorMessage)
    {
        kilometrageDepart = 0;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(KilometrageDepartText))
        {
            errorMessage = "Le kilométrage départ est obligatoire.";
            return false;
        }

        var normalizedText = KilometrageDepartText.Trim();

        if (!int.TryParse(
                normalizedText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out kilometrageDepart))
        {
            errorMessage = "Le kilométrage départ doit être un entier.";
            return false;
        }

        if (kilometrageDepart < 0)
        {
            errorMessage = "Le kilométrage départ doit être supérieur ou égal à 0.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Restaure la sélection visuelle si un camion est déjà présent dans l'état courant.
    /// Cela évite de perdre la sélection lors d'un retour sur l'écran.
    /// </summary>
    private void RestoreCurrentCamionSelection()
    {
        var currentCamion = _appStateService.CurrentCamion;

        if (currentCamion is null)
        {
            return;
        }

        var selected = Camions.FirstOrDefault(camion =>
            string.Equals(camion.Dto.IdCamion, currentCamion.IdCamion, StringComparison.OrdinalIgnoreCase)
            && string.Equals(camion.Dto.CodeCamion, currentCamion.CodeCamion, StringComparison.OrdinalIgnoreCase));

        if (selected is not null)
        {
            SelectedCamion = selected;
        }
    }

    private void RefreshCommandStates()
    {
        if (LoadCamionsCommand is Command loadCommand)
        {
            loadCommand.ChangeCanExecute();
        }

        if (ContinueCommand is Command continueCommand)
        {
            continueCommand.ChangeCanExecute();
        }
    }
}

/// <summary>
/// Élément affichable pour un camion dans la liste de choix.
/// Il encapsule le DTO camion et porte l'état visuel de sélection de la carte.
/// </summary>
public sealed class CamionItemViewModel : ObservableObject
{
    private bool _isSelected;

    public CamionItemViewModel(CamionDto dto, Action<CamionItemViewModel> selectAction)
    {
        Dto = dto;
        SelectCommand = new Command(() => selectAction(this));
    }

    public CamionDto Dto { get; }

    public ICommand SelectCommand { get; }

    public string NomAffiche => Dto.NomAffiche;

    public string ImmatriculationText => string.IsNullOrWhiteSpace(Dto.Immatriculation)
        ? Dto.CodeCamion
        : Dto.Immatriculation;

    public string LibelleText => string.IsNullOrWhiteSpace(Dto.LibelleCamion)
        ? "Camion sans libellé"
        : Dto.LibelleCamion;

    public string CodeText => $"Code : {Dto.CodeCamion}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(CardBackgroundColor));
                OnPropertyChanged(nameof(CardBorderColor));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonBackgroundColor));
                OnPropertyChanged(nameof(ButtonTextColor));
                OnPropertyChanged(nameof(SelectionText));
                OnPropertyChanged(nameof(SelectionTextColor));
            }
        }
    }

    public Color CardBackgroundColor => IsSelected
        ? Color.FromArgb("#1E40AF")
        : Color.FromArgb("#1E293B");

    public Color CardBorderColor => IsSelected
        ? Color.FromArgb("#93C5FD")
        : Color.FromArgb("#334155");

    public string ButtonText => IsSelected ? "Sélectionné" : "Choisir";

    public Color ButtonBackgroundColor => IsSelected
        ? Color.FromArgb("#22C55E")
        : Color.FromArgb("#1E293B");

    public Color ButtonTextColor => IsSelected
        ? Color.FromArgb("#FFFFFF")
        : Color.FromArgb("#93C5FD");

    public string SelectionText => IsSelected
        ? "Camion actuellement sélectionné"
        : "Touchez la carte ou le bouton pour sélectionner";

    public Color SelectionTextColor => IsSelected
        ? Color.FromArgb("#BBF7D0")
        : Color.FromArgb("#94A3B8");
}
