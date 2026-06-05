using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Diagnostics;
using MobileSLI.Services.Navigation;
using MobileSLI.ViewModels;

namespace MobileSLI;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);

        var app = builder.Build();

        Services = app.Services;

        return app;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<AppStateService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DatabaseExportService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<DemoDataService>();
        services.AddSingleton<ConnectivityService>();
        services.AddSingleton<INavigationService, ShellNavigationService>();

        services.AddSingleton<ApiClient>();
        services.AddSingleton<HealthApiService>();
        services.AddSingleton<LivreursApiService>();
        services.AddSingleton<CamionsApiService>();
        services.AddSingleton<TourneesApiService>();
        services.AddSingleton<SynchronisationsApiService>();

        services.AddSingleton<SynchronisationService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<AccueilViewModel>();
        services.AddTransient<IdentificationLivreurViewModel>();
        services.AddTransient<ChoixCamionViewModel>();
        services.AddTransient<ChoixTourneeViewModel>();
        services.AddTransient<ConfirmationTourneeViewModel>();
        services.AddTransient<ListePointsLivraisonViewModel>();
        services.AddTransient<DetailPointLivraisonViewModel>();
        services.AddTransient<DechargementViewModel>();
        services.AddTransient<RecapitulatifTourneeViewModel>();
        services.AddTransient<SyncResultViewModel>();
        services.AddTransient<SyncErrorViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<AccueilPage>();
        services.AddTransient<IdentificationLivreurPage>();
        services.AddTransient<ChoixCamionPage>();
        services.AddTransient<ChoixTourneePage>();
        services.AddTransient<ConfirmationTourneePage>();
        services.AddTransient<ListePointsLivraisonPage>();
        services.AddTransient<DetailPointLivraisonPage>();
        services.AddTransient<DechargementPage>();
        services.AddTransient<RecapitulatifTourneePage>();
        services.AddTransient<SyncResultPage>();
        services.AddTransient<SyncErrorPage>();
    }
}
