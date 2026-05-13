using MobileSLI.Pages;

namespace MobileSLI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(IdentificationLivreurPage), typeof(IdentificationLivreurPage));
        Routing.RegisterRoute(nameof(ChoixTourneePage), typeof(ChoixTourneePage));
        Routing.RegisterRoute(nameof(ConfirmationTourneePage), typeof(ConfirmationTourneePage));
        Routing.RegisterRoute(nameof(ListePointsLivraisonPage), typeof(ListePointsLivraisonPage));
        Routing.RegisterRoute(nameof(DetailPointLivraisonPage), typeof(DetailPointLivraisonPage));
        Routing.RegisterRoute(nameof(DechargementPage), typeof(DechargementPage));
        Routing.RegisterRoute(nameof(RecapitulatifTourneePage), typeof(RecapitulatifTourneePage));
        Routing.RegisterRoute(nameof(SyncResultPage), typeof(SyncResultPage));
        Routing.RegisterRoute(nameof(SyncErrorPage), typeof(SyncErrorPage));
    }

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