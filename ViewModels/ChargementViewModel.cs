using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Models;
using TourneesMobile.Pages;
using TourneesMobile.Services;

namespace TourneesMobile.ViewModels;

public partial class ChargementViewModel : BaseViewModel
{
    private readonly DatabaseService _database;

    [ObservableProperty]
    private TourneeEntity? tournee;

    [ObservableProperty]
    private int nombreArrets;

    [ObservableProperty]
    private int totalRollsPrevus;

    [ObservableProperty]
    private int totalTapisPrevus;

    [ObservableProperty]
    private int totalSacsPrevus;

    [ObservableProperty]
    private string? commentaireGlobal;

    public bool PeutModifier => Tournee is not { EstVerrouillee: true };

    public ChargementViewModel(DatabaseService database)
    {
        _database = database;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RunSafeAsync(async () =>
        {
            Tournee = await _database.GetTourneeActiveAsync();
            if (Tournee is null)
                return;

            CommentaireGlobal = Tournee.CommentaireGlobal;
            var arrets = await _database.GetArretsAsync(Tournee.IdTourneeLocale);
            NombreArrets = arrets.Count;
            TotalRollsPrevus = arrets.Sum(a => a.NbRolls);
            TotalTapisPrevus = arrets.Sum(a => a.NbTapis);
            TotalSacsPrevus = arrets.Sum(a => a.NbSacs);
            OnPropertyChanged(nameof(PeutModifier));
        });
    }

    [RelayCommand]
    private async Task ContinuerAsync()
    {
        if (Tournee is null)
        {
            await Shell.Current.DisplayAlert("Aucune tournée", "Charge d'abord une tournée.", "OK");
            return;
        }

        await _database.SetCommentaireGlobalAsync(Tournee.IdTourneeLocale, CommentaireGlobal);
        await Shell.Current.GoToAsync(nameof(ListeArretsPage));
    }

    [RelayCommand]
    private async Task RetourAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
