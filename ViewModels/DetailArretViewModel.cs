using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Models;
using TourneesMobile.Services;

namespace TourneesMobile.ViewModels;

public partial class DetailArretViewModel : BaseViewModel, IQueryAttributable
{
    private readonly DatabaseService _database;
    private string? _idLigneSource;

    [ObservableProperty]
    private ArretEntity? arret;

    [ObservableProperty]
    private bool estVerrouillee;

    public bool PeutModifier => !EstVerrouillee;
    public bool EstFait => Arret?.StatutPassage == StatutPassage.Fait;
    public bool EstNonFait => Arret?.StatutPassage == StatutPassage.NonFait;
    public bool EstAnomalie => Arret?.StatutPassage == StatutPassage.Anomalie;

    public DetailArretViewModel(DatabaseService database)
    {
        _database = database;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("idLigneSource", out var value))
            _idLigneSource = Uri.UnescapeDataString(value?.ToString() ?? string.Empty);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RunSafeAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(_idLigneSource))
                throw new InvalidOperationException("Identifiant de ligne absent.");

            Arret = await _database.GetArretAsync(_idLigneSource)
                ?? throw new InvalidOperationException("Arrêt introuvable.");

            var tournee = await _database.GetTourneeActiveAsync();
            EstVerrouillee = tournee?.EstVerrouillee ?? false;
            NotifyStatuts();
            OnPropertyChanged(nameof(PeutModifier));
        });
    }

    [RelayCommand]
    private void Increment(string champ)
    {
        if (Arret is null || EstVerrouillee)
            return;

        switch (champ)
        {
            case nameof(Arret.NbExpes): Arret.NbExpes++; break;
            case nameof(Arret.NbRolls): Arret.NbRolls++; break;
            case nameof(Arret.NbVetements): Arret.NbVetements++; break;
            case nameof(Arret.NbTapis): Arret.NbTapis++; break;
            case nameof(Arret.NbSacs): Arret.NbSacs++; break;
            case nameof(Arret.NbRecuperes): Arret.NbRecuperes++; break;
        }

        OnPropertyChanged(nameof(Arret));
    }

    [RelayCommand]
    private void Decrement(string champ)
    {
        if (Arret is null || EstVerrouillee)
            return;

        switch (champ)
        {
            case nameof(Arret.NbExpes): Arret.NbExpes = Math.Max(0, Arret.NbExpes - 1); break;
            case nameof(Arret.NbRolls): Arret.NbRolls = Math.Max(0, Arret.NbRolls - 1); break;
            case nameof(Arret.NbVetements): Arret.NbVetements = Math.Max(0, Arret.NbVetements - 1); break;
            case nameof(Arret.NbTapis): Arret.NbTapis = Math.Max(0, Arret.NbTapis - 1); break;
            case nameof(Arret.NbSacs): Arret.NbSacs = Math.Max(0, Arret.NbSacs - 1); break;
            case nameof(Arret.NbRecuperes): Arret.NbRecuperes = Math.Max(0, Arret.NbRecuperes - 1); break;
        }

        OnPropertyChanged(nameof(Arret));
    }

    [RelayCommand]
    private void ChoisirStatut(string statut)
    {
        if (Arret is null || EstVerrouillee)
            return;

        Arret.StatutPassage = statut;
        Arret.EstValidee = false;
        Arret.HeureValidation = null;
        NotifyStatuts();
        OnPropertyChanged(nameof(Arret));
    }

    [RelayCommand]
    private async Task ValiderAsync()
    {
        if (Arret is null)
            return;

        if (EstVerrouillee)
        {
            await Shell.Current.DisplayAlert("Tournée verrouillée", "Cette tournée a déjà été synchronisée.", "OK");
            return;
        }

        await RunSafeAsync(async () =>
        {
            if (Arret.StatutPassage == StatutPassage.AFaire)
                Arret.StatutPassage = StatutPassage.Fait;

            if (StatutPassage.DemandeCommentaire(Arret.StatutPassage) && string.IsNullOrWhiteSpace(Arret.CommentaireLivreur))
                throw new InvalidOperationException("Un commentaire est obligatoire pour un statut NON_FAIT ou ANOMALIE.");

            Arret.HeureValidation = DateTime.Now;
            Arret.EstValidee = true;
            await _database.UpdateArretAsync(Arret);

            await Shell.Current.DisplayAlert("Arrêt validé", "La saisie a été enregistrée localement.", "OK");
            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task EnregistrerSansValiderAsync()
    {
        if (Arret is null)
            return;

        await RunSafeAsync(async () =>
        {
            await _database.UpdateArretAsync(Arret);
            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task RetourAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void NotifyStatuts()
    {
        OnPropertyChanged(nameof(EstFait));
        OnPropertyChanged(nameof(EstNonFait));
        OnPropertyChanged(nameof(EstAnomalie));
    }
}
