namespace MobileSLI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        /*
         * Les téléphones livreurs Crosscall Core-M5 sont en thème système clair.
         * L'application MobileSLI utilise une interface sombre : on force donc
         * le thème sombre pour éviter les textes clairs sur fond blanc.
         */
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
