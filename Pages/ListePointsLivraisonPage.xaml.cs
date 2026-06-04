using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using MobileSLI.ViewModels;
using System.Collections.Generic;

namespace MobileSLI.Pages;

public partial class ListePointsLivraisonPage : ContentPage, IQueryAttributable
{
    private ListePointsLivraisonViewModel ViewModel => (ListePointsLivraisonViewModel)BindingContext;

    public ListePointsLivraisonPage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<ListePointsLivraisonViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("filtre", out var filtre))
        {
            ViewModel.ApplyNavigationFilter(filtre?.ToString());
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }
}
