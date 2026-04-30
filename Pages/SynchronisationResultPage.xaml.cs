using Microsoft.Maui.Controls;
using TourneesMobile;
using TourneesMobile.ViewModels;

namespace TourneesMobile.Pages;

public partial class SynchronisationResultPage : ContentPage, IQueryAttributable
{
    private SynchronisationResultViewModel ViewModel => (SynchronisationResultViewModel)BindingContext;

    public SynchronisationResultPage()
    {
        InitializeComponent();
        BindingContext = AppServices.Get<SynchronisationResultViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ViewModel.ApplyQueryAttributes(query);
    }
}
