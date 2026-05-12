using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class DechargementPage : ContentPage
{
    private DechargementViewModel ViewModel => (DechargementViewModel)BindingContext;

    public DechargementPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<DechargementViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Page critique : on force la navigation par les boutons visibles.
        return true;
    }
}
