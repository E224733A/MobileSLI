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
}
