using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
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

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        InfoText = string.Empty;
        Quantites.Clear();

        _ligne = await _databaseService.GetLigneAsync(_appStateService.SelectedLigneId);
        if (_ligne is null)
        {
            ErrorMessage = "Point de livraison introuvable.";
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
    }

    private void SetStatut(string? statut)
    {
        if (string.IsNullOrWhiteSpace(statut))
        {
            return;
        }

        SelectedStatut = statut;
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
