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

    private void SelectCamion(CamionItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedCamion = item;
        ErrorMessage = string.Empty;
    }

    private async Task ContinueAsync()
    {
        ErrorMessage = string.Empty;

        if (_appStateService.CurrentLivreur is null)
        {
            ErrorMessage = "Aucun livreur sélectionné. Revenez à l'identification.";
            return;
        }

        if (SelectedCamion is null)
        {
            ErrorMessage = "Sélectionnez un camion pour continuer.";
            return;
        }

        if (!TryValidateKilometrageDepart(out var kilometrageDepart, out var errorMessage))
        {
            ErrorMessage = errorMessage;
            return;
        }

        _appStateService.CurrentCamion = SelectedCamion.Dto;
        _appStateService.KilometrageDepart = kilometrageDepart;
        _appStateService.DateDepartMobile = DateTime.Now;

        await _navigationService.GoToAsync(nameof(ChoixTourneePage));
    }

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
