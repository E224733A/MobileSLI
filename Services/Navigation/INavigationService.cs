namespace MobileSLI.Services.Navigation;

/// <summary>
/// Abstraction de navigation utilisée par les ViewModels.
/// Elle permet de remplacer l'accès direct à Shell.Current.GoToAsync(...)
/// et rend les ViewModels plus simples à tester.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route);

    Task GoBackAsync();
}
