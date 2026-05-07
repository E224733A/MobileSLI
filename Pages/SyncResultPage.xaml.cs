using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class SyncResultPage : ContentPage
{
    private SyncResultViewModel ViewModel => (SyncResultViewModel)BindingContext;

    public SyncResultPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<SyncResultViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.Refresh();
    }
}