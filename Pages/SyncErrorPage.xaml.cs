using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class SyncErrorPage : ContentPage
{
    private SyncErrorViewModel ViewModel => (SyncErrorViewModel)BindingContext;

    public SyncErrorPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<SyncErrorViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.Refresh();
    }
}
