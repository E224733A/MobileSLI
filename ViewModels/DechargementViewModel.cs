using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class DechargementViewModel : BaseViewModel
{
    private readonly AppStateService _appStateService;
    private readonly DatabaseService _databaseService;

    public DechargementViewModel(AppStateService appStateService, DatabaseService databaseService)
    {
        _appStateService = appStateService;
        _databaseService = databaseService;
        LoadingMessage = "Chargement du déchargement...";
        Items = new ObservableCollection<DechargementItemViewModel>();
        GoRecapCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(RecapitulatifTourneePage)), () => !IsBusy);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."), () => !IsBusy);
    }

    public ObservableCollection<DechargementItemViewModel> Items { get; }

    public string CountText => $"{Items.Count} clients avec récupération";

    public ICommand GoRecapCommand { get; }

    public ICommand BackCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            LoadingMessage = "Chargement du déchargement...";
            SetBusy(true);
            await Task.Yield();

            Items.Clear();

            var lignes = await _databaseService.GetLignesAsync(_appStateService.CurrentTourneeId);

            foreach (var ligne in lignes.OrderBy(l => l.NomClient).ThenBy(l => l.OrdreArret))
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
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;

        if (GoRecapCommand is Command recapCommand)
        {
            recapCommand.ChangeCanExecute();
        }

        if (BackCommand is Command backCommand)
        {
            backCommand.ChangeCanExecute();
        }
    }
}
