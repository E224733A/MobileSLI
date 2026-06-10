using MobileSLI.Pages;

namespace MobileSLI;

/// <summary>
/// Shell principal de l'application MobileSLI.
/// Il centralise les routes de navigation et le blocage du bouton retour Android,
/// deux points sensibles pour éviter les sorties involontaires pendant une tournée chargée.
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Routes déclarées une seule fois afin que les ViewModels puissent naviguer par nom de page.
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
    /// Bloque le bouton retour physique Android.
    /// Cette règle est volontairement globale, car un retour arrière au mauvais moment peut quitter une tournée chargée
    /// ou contourner les boutons de navigation prévus dans le flux métier.
    /// </summary>
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
