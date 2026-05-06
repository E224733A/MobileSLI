using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MobileSLI.Models;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class ChoixTourneeViewModel : BaseViewModel
{
    private readonly DemoDataService _demoDataService;
    private readonly AppStateService _appStateService;

    private string _searchText = string.Empty;
    private TourneeListItemViewModel? _selectedTournee;

    public ChoixTourneeViewModel(DemoDataService demoDataService, AppStateService appStateService)
    {
        _demoDataService = demoDataService;
        _appStateService = appStateService;

        Tournees = new ObservableCollection<TourneeListItemViewModel>();
        SelectTourneeCommand = new Command<TourneeListItemViewModel>(SelectTournee);
        ContinueCommand = new Command(async () => await ContinueAsync(), () => SelectedTournee is not null);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public ObservableCollection<TourneeListItemViewModel> Tournees { get; }

    public string DateText => DateTime.Today.ToString("dd/MM/yyyy");
    public string LivreurText => _appStateService.CurrentLivreur?.NomLivreur ?? "Livreur non identifié";
    public string CountText => $"{Tournees.Count} tournées";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                LoadTournees();
            }
        }
    }

    public TourneeListItemViewModel? SelectedTournee
    {
        get => _selectedTournee;
        set
        {
            if (SetProperty(ref _selectedTournee, value))
            {
                ((Command)ContinueCommand).ChangeCanExecute();
            }
        }
    }

    public ICommand SelectTourneeCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand BackCommand { get; }

    public void LoadTournees()
    {
        Tournees.Clear();
        var items = _demoDataService.GetTourneesDisponibles();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            items = items
                .Where(t => t.CodeTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                            || t.LibelleTournee.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var item in items)
        {
            Tournees.Add(new TourneeListItemViewModel(item));
        }

        OnPropertyChanged(nameof(CountText));
    }

    private void SelectTournee(TourneeListItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedTournee = item;
        _appStateService.SelectedTournee = item.Dto;
    }

    private async Task ContinueAsync()
    {
        if (SelectedTournee is null)
        {
            ErrorMessage = "Sélectionnez une tournée.";
            return;
        }

        _appStateService.SelectedTournee = SelectedTournee.Dto;
        await Shell.Current.GoToAsync(nameof(ConfirmationTourneePage));
    }
}
