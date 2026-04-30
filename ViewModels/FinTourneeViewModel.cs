using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Models;
using TourneesMobile.Pages;
using TourneesMobile.Services;

namespace TourneesMobile.ViewModels;

public partial class FinTourneeViewModel : BaseViewModel
{
    private readonly DatabaseService _database;
    private readonly ApiService _api;

    [ObservableProperty]
    private TourneeEntity? tournee;

    [ObservableProperty]
    private int nombreTotal;

    [ObservableProperty]
    private int nombreValides;

    [ObservableProperty]
    private int nombreNonFaits;

    [ObservableProperty]
    private int nombreAnomalies;

    [ObservableProperty]
    private string? commentaireGlobal;

    [ObservableProperty]
    private string? jsonPreview;

    public int NombreRestants => Math.Max(0, NombreTotal - NombreValides);
    public bool PeutSynchroniser => Tournee is not null && !Tournee.EstVerrouillee && NombreTotal > 0 && NombreRestants == 0;

    public FinTourneeViewModel(DatabaseService database, ApiService api)
    {
        _database = database;
        _api = api;
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

            NombreTotal = arrets.Count;
            NombreValides = arrets.Count(a => a.EstValidee);
            NombreNonFaits = arrets.Count(a => a.StatutPassage == StatutPassage.NonFait);
            NombreAnomalies = arrets.Count(a => a.StatutPassage == StatutPassage.Anomalie);
            JsonPreview = PeutSynchroniser ? await _database.BuildSynchronisationJsonPreviewAsync(Tournee.IdTourneeLocale) : null;
            NotifyComputed();
        });
    }

    [RelayCommand]
    private async Task SynchroniserAsync()
    {
        if (Tournee is null)
            return;

        if (!PeutSynchroniser)
        {
            await Shell.Current.DisplayAlert("Synchronisation impossible", "Tous les arrêts doivent être validés avant l'envoi.", "OK");
            return;
        }

        var confirmation = await Shell.Current.DisplayAlert(
            "Envoi définitif",
            "Après synchronisation réussie, la tournée sera verrouillée et ne sera plus modifiable côté livreur.",
            "Envoyer",
            "Annuler");

        if (!confirmation)
            return;

        await RunSafeAsync(async () =>
        {
            await _database.SetCommentaireGlobalAsync(Tournee.IdTourneeLocale, CommentaireGlobal);
            var request = await _database.BuildSynchronisationRequestAsync(Tournee.IdTourneeLocale);
            var result = await _api.SynchroniserTourneeAsync(request);

            if (result.Success)
            {
                await _database.MarkSynchroniseeAsync(Tournee.IdTourneeLocale, result.IdSynchronisation ?? request.IdSynchronisation);
                await Shell.Current.GoToAsync($"{nameof(SynchronisationResultPage)}?success=true&message={Uri.EscapeDataString(result.Message ?? "Tournée synchronisée.")}");
                return;
            }

            await _database.MarkErreurAsync(Tournee.IdTourneeLocale);
            var message = result.Message ?? "La synchronisation a échoué.";
            if (result.Errors.Count > 0)
                message += Environment.NewLine + string.Join(Environment.NewLine, result.Errors);
            await Shell.Current.GoToAsync($"{nameof(SynchronisationResultPage)}?success=false&message={Uri.EscapeDataString(message)}");
        });
    }

    [RelayCommand]
    private async Task RetourListeAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void NotifyComputed()
    {
        OnPropertyChanged(nameof(NombreRestants));
        OnPropertyChanged(nameof(PeutSynchroniser));
    }
}
