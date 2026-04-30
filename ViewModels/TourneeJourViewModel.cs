using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Models;
using TourneesMobile.Pages;
using TourneesMobile.Services;

namespace TourneesMobile.ViewModels;

public partial class TourneeJourViewModel : BaseViewModel
{
    private readonly SettingsService _settings;
    private readonly ApiService _api;
    private readonly DatabaseService _database;
    private readonly DemoDataService _demo;

    [ObservableProperty]
    private string apiBaseUrl;

    [ObservableProperty]
    private DateTime dateTournee = DateTime.Today;

    [ObservableProperty]
    private string codeTournee;

    [ObservableProperty]
    private string codeLivreur;

    [ObservableProperty]
    private string nomLivreur;

    [ObservableProperty]
    private TourneeEntity? tourneeActive;

    [ObservableProperty]
    private int nombreArrets;

    [ObservableProperty]
    private int nombreValides;

    [ObservableProperty]
    private bool hasTourneeLocale;

    public string DateTourneeApi => DateTournee.ToString("yyyy-MM-dd");

    public TourneeJourViewModel(SettingsService settings, ApiService api, DatabaseService database, DemoDataService demo)
    {
        _settings = settings;
        _api = api;
        _database = database;
        _demo = demo;

        apiBaseUrl = _settings.ApiBaseUrl;
        codeTournee = _settings.LastCodeTournee;
        codeLivreur = _settings.LastCodeLivreur;
        nomLivreur = _settings.LastNomLivreur;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RunSafeAsync(async () =>
        {
            TourneeActive = await _database.GetTourneeActiveAsync();

            if (TourneeActive is not null)
            {
                var arrets = await _database.GetArretsAsync(TourneeActive.IdTourneeLocale);
                NombreArrets = arrets.Count;
                NombreValides = arrets.Count(a => a.EstValidee);
                HasTourneeLocale = true;
                StatusMessage = TourneeActive.EstVerrouillee
                    ? "Tournée déjà synchronisée et verrouillée."
                    : "Tournée locale chargée.";
            }
            else
            {
                HasTourneeLocale = false;
                NombreArrets = 0;
                NombreValides = 0;
                StatusMessage = "Aucune tournée locale chargée.";
            }
        });
    }

    [RelayCommand]
    private async Task ChargerDepuisApiAsync()
    {
        await RunSafeAsync(async () =>
        {
            SaveSettings();
            var dto = await _api.GetTourneeJourAsync(DateTourneeApi, CodeTournee, CodeLivreur);

            if (string.IsNullOrWhiteSpace(dto.Livreur.NomLivreur) && !string.IsNullOrWhiteSpace(NomLivreur))
                dto.Livreur.NomLivreur = NomLivreur;

            await _database.SaveTourneeFromApiAsync(dto);
            await InitializeAsync();

            await Shell.Current.DisplayAlert("Chargement terminé", "La tournée est stockée sur le téléphone.", "OK");
            await Shell.Current.GoToAsync(nameof(ChargementPage));
        });
    }

    [RelayCommand]
    private async Task ChargerDemoAsync()
    {
        await RunSafeAsync(async () =>
        {
            SaveSettings();

            var dto = _demo.CreateTourneeDemo();
            dto.DateTournee = DateTourneeApi;
            dto.CodeTournee = CodeTournee;
            dto.Livreur.CodeLivreur = CodeLivreur;
            dto.Livreur.NomLivreur = string.IsNullOrWhiteSpace(NomLivreur) ? "Livreur démo" : NomLivreur;

            await _database.SaveTourneeDemoAsync(dto);
            await InitializeAsync();

            await Shell.Current.DisplayAlert("Démo chargée", "Une tournée démo a été créée localement.", "OK");
            await Shell.Current.GoToAsync(nameof(ChargementPage));
        });
    }

    [RelayCommand]
    private async Task ReprendreTourneeAsync()
    {
        if (TourneeActive is null)
        {
            await Shell.Current.DisplayAlert("Aucune tournée", "Charge d'abord une tournée.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(ListeArretsPage));
    }

    [RelayCommand]
    private async Task OuvrirChargementAsync()
    {
        if (TourneeActive is null)
        {
            await Shell.Current.DisplayAlert("Aucune tournée", "Charge d'abord une tournée.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(ChargementPage));
    }

    private void SaveSettings()
    {
        _settings.ApiBaseUrl = ApiBaseUrl;
        _settings.LastCodeTournee = CodeTournee;
        _settings.LastCodeLivreur = CodeLivreur;
        _settings.LastNomLivreur = NomLivreur;
    }
}
