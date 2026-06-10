using Microsoft.Maui.Controls;

namespace MobileSLI.Services.Navigation;

/// <summary>
/// Implémentation MAUI Shell de la navigation applicative.
/// Les ViewModels passent par cette classe plutôt que par Shell.Current directement,
/// afin de garder la navigation centralisée et plus facile à remplacer en test.
/// </summary>
public sealed class ShellNavigationService : INavigationService
{
    /// <summary>
    /// Navigue vers une route déclarée dans AppShell.
    /// </summary>
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    /// <summary>
    /// Revient à l'écran précédent dans la pile de navigation Shell.
    /// </summary>
    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
