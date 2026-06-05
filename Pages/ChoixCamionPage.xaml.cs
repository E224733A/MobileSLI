using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class ChoixCamionPage : ContentPage
{
    public ChoixCamionPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ChoixCamionViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ChoixCamionViewModel viewModel)
        {
            await viewModel.LoadCamionsAsync();
        }
    }
}
