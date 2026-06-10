namespace MobileSLI;

/// <summary>
/// Entry point of the MobileSLI application. This partial class derives from the MAUI Application class.
/// It is responsible for initializing application components and creating the initial window.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Constructs the application and initializes its components.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Overrides CreateWindow to create and return the main application window using AppShell.
    /// </summary>
    /// <param name="activationState">Activation state provided by the MAUI framework.</param>
    /// <returns>A new Window instance containing the AppShell.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
