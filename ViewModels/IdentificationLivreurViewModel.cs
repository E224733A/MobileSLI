using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

public sealed class IdentificationLivreurViewModel : BaseViewModel
{
    private readonly LivreursApiService _livreursApiService;
    private readonly AppStateService _appStateService;
    private readonly SettingsService _settingsService;
    private readonly INavigationService _navigationService;

    private string _codeLivreur = string.Empty;
    private string _nomLivreur = string.Empty;
    private bool _isLivreurRecognized;
    private bool _isLoading;
    private bool _hasLoaded;
    private LivreurItemViewModel? _selectedLivreur;

    public IdentificationLivreurViewModel(
        LivreursApiService livreursApiService,
        AppStateService appStateService,
        SettingsService settingsService,
        INavigationService navigationService)
    {
        _livreursApiService = livreursApiService;
        _appStateService = appStateService;
        _settingsService = settingsService;
        _navigationService = navigationService;

        _codeLivreur = _settingsService.LastLivreurCode;

        Livreurs = new ObservableCollection<LivreurItemViewModel>();

        LoadLivreursCommand = new Command(
            async () => await LoadLivreursAsync(forceReload: true),
            () => !IsLoading);

        ValidateCommand = new Command(
            async () => await ValidateLivreurAsync(),
            () => !IsLoading);

        ContinueCommand = new Command(
            async () => await ContinueAsync(),
            () => IsLivreurRecognized && !IsLoading);
    }

    public ObservableCollection<LivreurItemViewModel> Livreurs { get; }

    public string CodeLivreur
    {
        get => _codeLivreur;
        set
        {
            if (SetProperty(ref _codeLivreur, value))
            {
                ClearSelectedLivreur();
            }
        }
    }

    public string NomLivreur
    {
        get => _nomLivreur;
        set => SetProperty(ref _nomLivreur, value);
    }

    public LivreurItemViewModel? SelectedLivreur
    {
        get => _selectedLivreur;
        set
        {
            if (SetProperty(ref _selectedLivreur, value) && value is not null)
            {
                SelectLivreur(value);
            }
        }
    }

    public bool IsLivreurRecognized
    {
        get => _isLivreurRecognized;
        set
        {
            if (SetProperty(ref _isLivreurRecognized, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public ICommand LoadLivreursCommand { get; }

    public ICommand ValidateCommand { get; }

    public ICommand ContinueCommand { get; }

    public async Task LoadLivreursAsync(bool forceReload = false)
    {
        if (IsLoading)
        {
            return;
        }

        if (_hasLoaded && !forceReload)
        {
            TrySelectLastLivreur();
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            Livreurs.Clear();

            var livreursApi = await _livreursApiService.GetLivreursAsync();

            foreach (var livreur in livreursApi)
            {
                if (string.IsNullOrWhiteSpace(livreur.CodeLivreur))
                {
                    continue;
                }

                Livreurs.Add(new LivreurItemViewModel
                {
                    CodeLivreur = livreur.CodeLivreur.Trim(),
                    NomLivreur = livreur.NomLivreur?.Trim() ?? string.Empty
                });
            }

            _hasLoaded = true;

            if (Livreurs.Count == 0)
            {
                ErrorMessage = "Aucun livreur n'a été renvoyé par l'API.";
                return;
            }

            TrySelectLastLivreur();
        }
        catch (ApiClientException exception)
        {
            ErrorMessage =
                $"Impossible de charger les livreurs depuis l'API. " +
                $"Route : {exception.Route}. " +
                $"Code HTTP : {exception.StatusCode}.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible de charger les livreurs : {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ValidateLivreurAsync()
    {
        ErrorMessage = string.Empty;

        if (Livreurs.Count == 0)
        {
            await LoadLivreursAsync(forceReload: true);
        }

        ClearSelectedLivreur();

        if (string.IsNullOrWhiteSpace(CodeLivreur))
        {
            ErrorMessage = "Le code livreur est obligatoire.";
            return;
        }

        var normalizedCode = CodeLivreur.Trim();

        var livreur = Livreurs.FirstOrDefault(l =>
            string.Equals(l.CodeLivreur, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (livreur is null)
        {
            ErrorMessage = "Le code livreur est inconnu dans la liste renvoyée par l'API.";
            return;
        }

        if (!ReferenceEquals(SelectedLivreur, livreur))
        {
            SelectedLivreur = livreur;
        }
        else
        {
            SelectLivreur(livreur);
        }
    }

    private async Task ContinueAsync()
    {
        if (_appStateService.CurrentLivreur is null)
        {
            await ValidateLivreurAsync();
        }

        if (_appStateService.CurrentLivreur is not null)
        {
            await _navigationService.GoToAsync(nameof(ChoixTourneePage));
        }
    }

    private void SelectLivreur(LivreurItemViewModel livreur)
    {
        ErrorMessage = string.Empty;

        SetProperty(ref _codeLivreur, livreur.CodeLivreur, nameof(CodeLivreur));
        NomLivreur = livreur.NomLivreur;

        _appStateService.CurrentLivreur = new LivreurDto
        {
            CodeLivreur = livreur.CodeLivreur,
            NomLivreur = livreur.NomLivreur
        };

        _appStateService.SelectedTournee = null;
        _appStateService.CurrentTourneeId = 0;
        _appStateService.SelectedLigneId = 0;

        _settingsService.LastLivreurCode = livreur.CodeLivreur;

        IsLivreurRecognized = true;
    }

    private void ClearSelectedLivreur()
    {
        IsLivreurRecognized = false;
        NomLivreur = string.Empty;
        _appStateService.CurrentLivreur = null;
        _appStateService.SelectedTournee = null;
        _appStateService.CurrentTourneeId = 0;
        _appStateService.SelectedLigneId = 0;
    }

    private void TrySelectLastLivreur()
    {
        if (string.IsNullOrWhiteSpace(_settingsService.LastLivreurCode))
        {
            return;
        }

        var lastLivreur = Livreurs.FirstOrDefault(l =>
            string.Equals(
                l.CodeLivreur,
                _settingsService.LastLivreurCode,
                StringComparison.OrdinalIgnoreCase));

        if (lastLivreur is not null)
        {
            SelectedLivreur = lastLivreur;
            SelectLivreur(lastLivreur);
        }
    }

    private void RefreshCommandStates()
    {
        if (LoadLivreursCommand is Command loadCommand)
        {
            loadCommand.ChangeCanExecute();
        }

        if (ValidateCommand is Command validateCommand)
        {
            validateCommand.ChangeCanExecute();
        }

        if (ContinueCommand is Command continueCommand)
        {
            continueCommand.ChangeCanExecute();
        }
    }
}

public sealed class LivreurItemViewModel
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(NomLivreur)
            ? CodeLivreur
            : $"{CodeLivreur} - {NomLivreur}";
}
