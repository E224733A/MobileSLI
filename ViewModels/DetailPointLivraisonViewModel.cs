using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class DetailPointLivraisonViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;

    private LocalTourneeLigne? _ligne;
    private string _selectedStatut = StatutPassageConstants.Fait;
    private string _commentaireLivreur = string.Empty;
    private string _infoText = string.Empty;

    public DetailPointLivraisonViewModel(AppStateService appStateService, DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;

        Quantites = new ObservableCollection<QuantiteSaisieViewModel>();
        SetStatutCommand = new Command<string>(SetStatut);
        ValidateCommand = new Command(async () => await ValidateAsync());
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public ObservableCollection<QuantiteSaisieViewModel> Quantites { get; }

    public string ClientText => _ligne is null ? "Client" : $"{_ligne.NumClient} — {_ligne.NomClient}";
    public string PointText => _ligne?.DescriptionPDL ?? string.Empty;
    public string AdresseText => _ligne?.AdresseLigne1 ?? string.Empty;
    public string ZoneText => _ligne is null ? string.Empty : $"Zone : {_ligne.Zone ?? "-"}";
    public string OrdreText => _ligne is null ? string.Empty : $"Arrêt {_ligne.OrdreArret}";
    public string InstructionsText => _ligne?.Instructions ?? "Aucune instruction particulière.";

    public string SelectedStatut
    {
        get => _selectedStatut;
        set => SetProperty(ref _selectedStatut, value);
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
        OnPropertyChanged(nameof(OrdreText));
        OnPropertyChanged(nameof(InstructionsText));
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
        InfoText = $"Passage validé à {_ligne.HeureValidation:HH:mm}.";
    }
}
