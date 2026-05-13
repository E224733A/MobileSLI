using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;

namespace MobileSLI.Pages;

public partial class ConfirmationTourneePage : ContentPage
{
    public ConfirmationTourneePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ConfirmationTourneeViewModel>();
    }
}