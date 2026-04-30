using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Pages;

namespace TourneesMobile.ViewModels;

public partial class SynchronisationResultViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private bool success;

    [ObservableProperty]
    private string message = string.Empty;

    public string Titre => Success ? "Synchronisation réussie" : "Synchronisation échouée";
    public string SousTitre => Success
        ? "Les données ont été envoyées à l'API et la tournée est verrouillée."
        : "Les données restent stockées sur le téléphone. Une nouvelle tentative est possible depuis le dépôt.";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("success", out var successValue))
            Success = bool.TryParse(successValue?.ToString(), out var parsed) && parsed;

        if (query.TryGetValue("message", out var messageValue))
            Message = Uri.UnescapeDataString(messageValue?.ToString() ?? string.Empty);

        OnPropertyChanged(nameof(Titre));
        OnPropertyChanged(nameof(SousTitre));
    }

    [RelayCommand]
    private async Task RetourAccueilAsync()
    {
        await Shell.Current.GoToAsync("//tournee");
    }

    [RelayCommand]
    private async Task RetourFinTourneeAsync()
    {
        await Shell.Current.GoToAsync(nameof(FinTourneePage));
    }
}
