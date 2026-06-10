using MobileSLI.Pages;

namespace MobileSLI;

/// <summary>
/// Represents the shell of the MobileSLI application. It registers navigation routes for all pages
/// and customizes shell behavior such as disabling the hardware back button on Android.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Initializes the shell and registers navigation routes for each page.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Register routes to pages so they can be navigated to by name.
        Routing.RegisterRoute(nameof(IdentificationLivreurPage), typeof(IdentificationLivreurPage));
        Routing.RegisterRoute(nameof(ChoixCamionPage), typeof(ChoixCamionPage));
        Routing.RegisterRoute(nameof(ChoixTourneePage), typeof(ChoixTourneePage));
        Routing.RegisterRoute(nameof(ConfirmationTourneePage), typeof(ConfirmationTourneePage));
        Routing.RegisterRoute(nameof(ListePointsLivraisonPage), typeof(ListePointsLivraisonPage));
        Routing.RegisterRoute(nameof(DetailPointLivraisonPage), typeof(DetailPointLivraisonPage));
        Routing.RegisterRoute(nameof(DechargementPage), typeof(DechargementPage));
        Routing.RegisterRoute(nameof(RecapitulatifTourneePage), typeof(RecapitulatifTourneePage));
        Routing.RegisterRoute(nameof(SyncResultPage), typeof(SyncResultPage));
        Routing.RegisterRoute(nameof(SyncErrorPage), typeof(SyncErrorPage));
    }

    /// <summary>
    /// Disables the Android hardware back button to avoid accidental navigation or exiting from a loaded route.
    /// Returns true to indicate the event was handled and should not propagate.
    /// </summary>
    /// <returns>Always returns true to consume the back button event.</returns>
    protected override bool OnBackButtonPressed()
    {
        /*
         * Blocage global du bouton retour Android.
         *
         * Objectif :
         * - éviter les retours accidentels ;
         * - empêcher la sortie involontaire d'une tournée chargée ;
         * - forcer l'utilisateur à utiliser les boutons prévus dans l'application ;
         * - éviter la duplication de code dans chaque page.
         *
         * Les boutons internes de l'application restent fonctionnels :
         * - Retour
         * - Continuer
         * - Reprendre
         * - Récapitulatif
         * - Envoyer
         */
        return true;
    }
}
