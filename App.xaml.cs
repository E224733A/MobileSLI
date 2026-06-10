namespace MobileSLI;

/// <summary>
/// Point d'entrée de l'application mobile MobileSLI.
/// Cette classe reste volontairement minimale : l'initialisation visuelle est faite par MAUI,
/// tandis que les routes de navigation sont centralisées dans AppShell.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Crée la fenêtre principale en utilisant le Shell de l'application.
    /// Le Shell porte ensuite les règles de navigation, notamment le blocage du bouton retour Android.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
