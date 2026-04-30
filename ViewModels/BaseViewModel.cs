using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TourneesMobile.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? statusMessage;

    public async Task RunSafeAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        try
        {
            ErrorMessage = null;
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await Shell.Current.DisplayAlert("Erreur", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
