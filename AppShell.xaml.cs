using TourneesMobile.Pages;

namespace TourneesMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ChargementPage), typeof(ChargementPage));
        Routing.RegisterRoute(nameof(ListeArretsPage), typeof(ListeArretsPage));
        Routing.RegisterRoute(nameof(DetailArretPage), typeof(DetailArretPage));
        Routing.RegisterRoute(nameof(FinTourneePage), typeof(FinTourneePage));
        Routing.RegisterRoute(nameof(SynchronisationResultPage), typeof(SynchronisationResultPage));
    }
}
