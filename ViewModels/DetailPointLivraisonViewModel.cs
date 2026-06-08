using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Services;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

public sealed class DetailPointLivraisonViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    private LocalTourneeLigne? _ligne;
    private string _selectedStatut = StatutPassageConstants.Fait;
    private string _commentaireLivreur = string.Empty;
    private string _infoText = string.Empty;

    public DetailPointLivraisonViewModel(
        AppStateService appStateService,
        DatabaseService databaseService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _navigationService = navigationService;

        Quantites = new ObservableCollection<QuantiteSaisieViewModel>();
        SetStatutCommand = new Command<string>(SetStatut);
        ValidateCommand = new Command(async () => await ValidateAsync());
        BackCommand = new Command(async () => await _navigationService.GoBackAsync());
        OuvrirAdresseLivraisonCommand = new Command(
            async () => await OuvrirAdresseLivraisonAsync(),
            () => HasLienAdresseLivraison);
    }

    public ObservableCollection<QuantiteSaisieViewModel> Quantites { get; }

    public string ClientText => _ligne is null ? "Client" : $"{_ligne.NumClient} — {_ligne.NomClient}";

    public string PointText => _ligne?.DescriptionPDL ?? string.Empty;

    public string AdresseText => _ligne?.AdresseLigne1 ?? string.Empty;

    public string ZoneText => _ligne is null
        ? string.Empty
        : string.IsNullOrWhiteSpace(_ligne.ZoneDechargementAffichee)
            ? $"Zone : {_ligne.Zone ?? "-"}"
            : $"Zone : {_ligne.ZoneDechargementAffichee}";

    public bool IsFermetureVisible => _ligne?.EstFerme == true;

    public string FermetureText => _ligne?.FermetureText ?? string.Empty;

    public string OrdreText => _ligne is null ? string.Empty : $"Arrêt {_ligne.OrdreArret}";

    public bool HasInstructions => !string.IsNullOrWhiteSpace(_ligne?.Instructions);

    public string InstructionsText => _ligne?.Instructions ?? string.Empty;

    public bool HasCommentaireExceptionnel => !string.IsNullOrWhiteSpace(_ligne?.CommentaireExceptionnel);

    public string CommentaireExceptionnelText => _ligne?.CommentaireExceptionnel ?? string.Empty;

    public bool HasInformationsLivreur => HasInstructions || HasCommentaireExceptionnel;

    public bool HasLienAdresseLivraison =>
        HasValidGpsCoordinates(_ligne?.LatitudeLivraison, _ligne?.LongitudeLivraison)
        || TryCreateAdresseLivraisonUri(_ligne?.LienAdresseLivraison, out _);

    public string LienAdresseLivraisonText => HasLienAdresseLivraison
        ? "Ouvrir dans Maps"
        : string.Empty;

    public string SelectedStatut
    {
        get => _selectedStatut;
        set
        {
            if (SetProperty(ref _selectedStatut, value))
            {
                RefreshStatutButtonColors();
            }
        }
    }

    public string CommentaireLivreur
    {
        get => _commentaireLivreur;
        set => SetProperty(ref _commentaireLivreur, value);
    }

    public string InfoText
    {
        get => _infoText;
        set => SetProperty(ref _infoText, value);
    }

    public Color FaitButtonBackgroundColor => GetStatutButtonBackgroundColor(StatutPassageConstants.Fait);
    public Color NonFaitButtonBackgroundColor => GetStatutButtonBackgroundColor(StatutPassageConstants.NonFait);
    public Color AnomalieButtonBackgroundColor => GetStatutButtonBackgroundColor(StatutPassageConstants.Anomalie);

    public Color FaitButtonTextColor => GetStatutButtonTextColor(StatutPassageConstants.Fait);
    public Color NonFaitButtonTextColor => GetStatutButtonTextColor(StatutPassageConstants.NonFait);
    public Color AnomalieButtonTextColor => GetStatutButtonTextColor(StatutPassageConstants.Anomalie);

    public Color FaitButtonBorderColor => GetStatutButtonBorderColor(StatutPassageConstants.Fait);
    public Color NonFaitButtonBorderColor => GetStatutButtonBorderColor(StatutPassageConstants.NonFait);
    public Color AnomalieButtonBorderColor => GetStatutButtonBorderColor(StatutPassageConstants.Anomalie);

    public ICommand SetStatutCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand OuvrirAdresseLivraisonCommand { get; }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        InfoText = string.Empty;
        Quantites.Clear();

        _ligne = await _databaseService.GetLigneAsync(_appStateService.SelectedLigneId);
        if (_ligne is null)
        {
            ErrorMessage = "Point de livraison introuvable.";
            RefreshLienAdresseLivraisonState();
            return;
        }

        SelectedStatut = _ligne.StatutPassage == StatutPassageConstants.AFaire
            ? StatutPassageConstants.Fait
            : _ligne.StatutPassage;

        CommentaireLivreur = _ligne.CommentaireLivreur ?? string.Empty;

        var quantites = await _databaseService.GetQuantitesAsync(_ligne.Id);
        foreach (var quantite in quantites)
        {
            Quantites.Add(new QuantiteSaisieViewModel(quantite));
        }

        OnPropertyChanged(nameof(ClientText));
        OnPropertyChanged(nameof(PointText));
        OnPropertyChanged(nameof(AdresseText));
        OnPropertyChanged(nameof(ZoneText));
        OnPropertyChanged(nameof(IsFermetureVisible));
        OnPropertyChanged(nameof(FermetureText));
        OnPropertyChanged(nameof(OrdreText));
        OnPropertyChanged(nameof(HasInstructions));
        OnPropertyChanged(nameof(InstructionsText));
        OnPropertyChanged(nameof(HasCommentaireExceptionnel));
        OnPropertyChanged(nameof(CommentaireExceptionnelText));
        OnPropertyChanged(nameof(HasInformationsLivreur));
        RefreshLienAdresseLivraisonState();
    }

    private void SetStatut(string? statut)
    {
        if (string.IsNullOrWhiteSpace(statut))
        {
            return;
        }

        SelectedStatut = statut;
    }

    private async Task OuvrirAdresseLivraisonAsync()
    {
        ErrorMessage = string.Empty;
        InfoText = string.Empty;

        Uri? uri = null;

        if (HasValidGpsCoordinates(_ligne?.LatitudeLivraison, _ligne?.LongitudeLivraison))
        {
            uri = BuildGoogleMapsDirectionsUri(
                _ligne!.LatitudeLivraison!.Value,
                _ligne.LongitudeLivraison!.Value);
        }
        else if (!TryCreateAdresseLivraisonUri(_ligne?.LienAdresseLivraison, out uri))
        {
            await DisplayMessageAsync(
                "Adresse non disponible",
                "Aucune coordonnée GPS valide n'est disponible pour ce point de livraison.");
            return;
        }

        try
        {
            await Launcher.OpenAsync(uri);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Impossible d'ouvrir Maps : {exception.Message}";
            await DisplayMessageAsync("Ouverture impossible", "Impossible d'ouvrir l'adresse dans Maps.");
        }
    }

    private async Task ValidateAsync()
    {
        if (_ligne is null)
        {
            ErrorMessage = "Point de livraison introuvable.";
            return;
        }

        ErrorMessage = string.Empty;
        InfoText = string.Empty;

        if (SelectedStatut == StatutPassageConstants.AFaire)
        {
            ErrorMessage = "Choisissez un statut de passage avant de valider.";
            return;
        }

        if ((SelectedStatut == StatutPassageConstants.NonFait || SelectedStatut == StatutPassageConstants.Anomalie)
            && string.IsNullOrWhiteSpace(CommentaireLivreur))
        {
            ErrorMessage = "Un commentaire est obligatoire pour Non fait ou Anomalie.";
            return;
        }

        foreach (var quantite in Quantites)
        {
            if (!quantite.ApplyToEntity(out var error))
            {
                ErrorMessage = error;
                return;
            }
        }

        _ligne.StatutPassage = SelectedStatut;
        _ligne.EstValidee = true;
        _ligne.HeureValidation = DateTime.Now;
        _ligne.CommentaireLivreur = string.IsNullOrWhiteSpace(CommentaireLivreur) ? null : CommentaireLivreur.Trim();

        await _databaseService.SaveLigneAsync(_ligne, Quantites.Select(q => q.Entity));

        await _navigationService.GoBackAsync();
    }

    private static bool HasValidGpsCoordinates(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return false;
        }

        if (double.IsNaN(latitude.Value)
            || double.IsInfinity(latitude.Value)
            || double.IsNaN(longitude.Value)
            || double.IsInfinity(longitude.Value))
        {
            return false;
        }

        return latitude.Value >= -90
            && latitude.Value <= 90
            && longitude.Value >= -180
            && longitude.Value <= 180
            && !(latitude.Value == 0 && longitude.Value == 0);
    }

    private static Uri BuildGoogleMapsDirectionsUri(double latitude, double longitude)
    {
        var latitudeText = latitude.ToString(CultureInfo.InvariantCulture);
        var longitudeText = longitude.ToString(CultureInfo.InvariantCulture);

        return new Uri($"https://www.google.com/maps/dir/?api=1&destination={latitudeText},{longitudeText}&travelmode=driving");
    }

    private static bool TryCreateAdresseLivraisonUri(string? lien, out Uri uri)
    {
        uri = null!;

        if (string.IsNullOrWhiteSpace(lien))
        {
            return false;
        }

        var trimmed = lien.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var candidate))
        {
            return false;
        }

        if (string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return IsAllowedHttpsMapsUri(candidate, out uri);
        }

        if (string.Equals(candidate.Scheme, "geo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate.Scheme, "google.navigation", StringComparison.OrdinalIgnoreCase))
        {
            uri = candidate;
            return true;
        }

        return false;
    }

    private static bool IsAllowedHttpsMapsUri(Uri candidate, out Uri uri)
    {
        uri = null!;

        var host = candidate.Host.ToLowerInvariant();
        var path = candidate.AbsolutePath ?? string.Empty;

        var isAllowed = string.Equals(host, "maps.google.com", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(host, "www.google.com", StringComparison.OrdinalIgnoreCase)
                && path.StartsWith("/maps", StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    private async Task DisplayMessageAsync(string title, string message)
    {
        if (Shell.Current?.CurrentPage is null)
        {
            return;
        }

        await Shell.Current.CurrentPage.DisplayAlertAsync(title, message, "OK");
    }

    private void RefreshLienAdresseLivraisonState()
    {
        OnPropertyChanged(nameof(HasLienAdresseLivraison));
        OnPropertyChanged(nameof(LienAdresseLivraisonText));

        if (OuvrirAdresseLivraisonCommand is Command command)
        {
            command.ChangeCanExecute();
        }
    }

    private bool IsSelectedStatut(string statut)
    {
        return string.Equals(SelectedStatut, statut, StringComparison.OrdinalIgnoreCase);
    }

    private Color GetStatutButtonBackgroundColor(string statut)
    {
        return IsSelectedStatut(statut)
            ? Color.FromArgb("#DBEAFE")
            : Color.FromArgb("#1E293B");
    }

    private Color GetStatutButtonTextColor(string statut)
    {
        return IsSelectedStatut(statut)
            ? Color.FromArgb("#1D4ED8")
            : Color.FromArgb("#E2E8F0");
    }

    private Color GetStatutButtonBorderColor(string statut)
    {
        return IsSelectedStatut(statut)
            ? Color.FromArgb("#93C5FD")
            : Color.FromArgb("#334155");
    }

    private void RefreshStatutButtonColors()
    {
        OnPropertyChanged(nameof(FaitButtonBackgroundColor));
        OnPropertyChanged(nameof(NonFaitButtonBackgroundColor));
        OnPropertyChanged(nameof(AnomalieButtonBackgroundColor));

        OnPropertyChanged(nameof(FaitButtonTextColor));
        OnPropertyChanged(nameof(NonFaitButtonTextColor));
        OnPropertyChanged(nameof(AnomalieButtonTextColor));

        OnPropertyChanged(nameof(FaitButtonBorderColor));
    }
}
