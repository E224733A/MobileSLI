namespace MobileSLI.Services.Navigation;

/// <summary>
/// Abstraction de navigation utilisée par les ViewModels.
/// Elle évite de dépendre directement de Shell.Current dans la logique écran,
/// ce qui limite le couplage entre navigation MAUI et règles applicatives.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigue vers une route déclarée dans le Shell applicatif.
    /// </summary>
    Task GoToAsync(string route);

    /// <summary>
    /// Revient à l'écran précédent selon la pile de navigation MAUI Shell.
    /// </summary>
    Task GoBackAsync();
}
