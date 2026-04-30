using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class DetailArretPage : ContentPage, IQueryAttributable
{
    private DetailArretViewModel ViewModel => (DetailArretViewModel)BindingContext;

    public DetailArretPage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<DetailArretViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ViewModel.ApplyQueryAttributes(query);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeCommand.ExecuteAsync(null);
    }
}
