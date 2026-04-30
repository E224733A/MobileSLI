using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TourneesMobile.Models;
using TourneesMobile.Pages;
using TourneesMobile.Services;

namespace TourneesMobile.ViewModels;

public partial class ListeArretsViewModel : BaseViewModel
{
    private readonly DatabaseService _database;

    [ObservableProperty]
    private TourneeEntity? tournee;

    [ObservableProperty]
    private string recherche = string.Empty;

    public ObservableCollection<ArretEntity> Arrets { get; } = new();

    private List<ArretEntity> _source = [];

    public int NombreTotal => _source.Count;
    public int NombreValides => _source.Count(a => a.EstValidee);
    public int NombreRestants => Math.Max(0, NombreTotal - NombreValides);
    public double Progression => NombreTotal == 0 ? 0 : (double)NombreValides / NombreTotal;

    public ListeArretsViewModel(DatabaseService database)
    {
        _database = database;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RunSafeAsync(async () =>
        {
            Tournee = await _database.GetTourneeActiveAsync();
            Arrets.Clear();
            _source = [];

            if (Tournee is null)
                return;

            _source = await _database.GetArretsAsync(Tournee.IdTourneeLocale);
            ApplyFilter();
            NotifyCounters();
        });
    }

    partial void OnRechercheChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Arrets.Clear();

        var query = string.IsNullOrWhiteSpace(Recherche)
            ? _source
            : _source.Where(a =>
                a.NomAfficheCourt.Contains(Recherche, StringComparison.OrdinalIgnoreCase) ||
                a.NumClient.Contains(Recherche, StringComparison.OrdinalIgnoreCase) ||
                (a.CodePDL ?? string.Empty).Contains(Recherche, StringComparison.OrdinalIgnoreCase));

        foreach (var arret in query)
            Arrets.Add(arret);
    }

    [RelayCommand]
    private async Task OuvrirArretAsync(ArretEntity? arret)
    {
        if (arret is null)
            return;

        await Shell.Current.GoToAsync($"{nameof(DetailArretPage)}?idLigneSource={Uri.EscapeDataString(arret.IdLigneSource)}");
    }

    [RelayCommand]
    private async Task FinTourneeAsync()
    {
        await Shell.Current.GoToAsync(nameof(FinTourneePage));
    }

    [RelayCommand]
    private async Task RetourAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private void NotifyCounters()
    {
        OnPropertyChanged(nameof(NombreTotal));
        OnPropertyChanged(nameof(NombreValides));
        OnPropertyChanged(nameof(NombreRestants));
        OnPropertyChanged(nameof(Progression));
    }
}
