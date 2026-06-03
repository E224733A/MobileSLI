using Microsoft.Maui.Controls;

namespace MobileSLI.Services.Navigation;

/// <summary>
/// Implémentation MAUI Shell de la navigation applicative.
/// </summary>
public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
