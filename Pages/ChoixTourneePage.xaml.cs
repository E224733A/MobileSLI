using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class ChoixTourneePage : ContentPage
{
    private ChoixTourneeViewModel ViewModel => (ChoixTourneeViewModel)BindingContext;

    public ChoixTourneePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ChoixTourneeViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.LoadTournees();
    }
}
