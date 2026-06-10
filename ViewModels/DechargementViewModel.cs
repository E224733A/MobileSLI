using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Navigation;

namespace MobileSLI.ViewModels;

/// <summary>
/// ViewModel de l'écran de déchargement.
/// Il affiche une synthèse des articles récupérés pendant la tournée.
/// </summary>
public sealed class DechargementViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    public DechargementViewModel(
        AppStateService appStateService,
        DatabaseService databaseService,
        INavigationService navigationService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        _navigationService = navigationService;

        Items = new ObservableCollection<DechargementItemViewModel>();
        GoRecapCommand = new Command(async () => await _navigationService.GoToAsync(nameof(RecapitulatifTourneePage)));
        BackCommand = new Command(async () => await _navigationService.GoBackAsync());
    }

    public ObservableCollection<DechargementItemViewModel> Items { get; }

    public string CountText => $"{Items.Count} clients avec récupération";

    public ICommand GoRecapCommand { get; }

    public ICommand BackCommand { get; }

    /// <summary>
    /// Charge les lignes de la tournée courante et ne garde que les points
    /// qui possèdent au moins une quantité récupérée supérieure à zéro.
    /// </summary>
    public async Task LoadAsync()
    {
        Items.Clear();

        var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);

        foreach (var ligne in lignes.OrderBy(l => l.OrdreArret).ThenBy(l => l.NomClient))
        {
            var quantites = await _databaseService.GetQuantitesAsync(ligne.Id);
            var recuperees = quantites.Where(q => q.QuantiteRecuperee > 0).ToList();

            if (recuperees.Count == 0)
            {
                continue;
            }

            Items.Add(new DechargementItemViewModel
            {
                ClientText = $"{ligne.NumClient} - {ligne.NomClient}",
                PointText = ligne.DescriptionPDL ?? string.Empty,
                ZoneText = string.IsNullOrWhiteSpace(ligne.ZoneDechargementAffichee)
                    ? "Zone : non renseignée"
                    : $"Zone : {ligne.ZoneDechargementAffichee}",
                ArticlesText = string.Join(" · ", recuperees.Select(q => $"{q.QuantiteRecuperee} {q.Libelle}"))
            });
        }

        OnPropertyChanged(nameof(CountText));
    }
}
