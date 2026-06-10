using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MobileSLI.Pages;
using MobileSLI.Services;
using MobileSLI.Services.Api;
using MobileSLI.Services.Diagnostics;
using MobileSLI.Services.Navigation;
using MobileSLI.ViewModels;

namespace MobileSLI;

/// <summary>
/// Configures and builds the MAUI application. This static class sets up dependency injection
/// for all services, view models, and pages used throughout the MobileSLI app.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Service provider for the built application. Populated during app creation.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = default!;

    /// <summary>
    /// Creates and configures the MAUI application.
    /// </summary>
    /// <returns>A fully built <see cref="MauiApp"/> instance.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // Register the main application class.
        builder
            .UseMauiApp<App>();

#if DEBUG
        // When debugging, enable debug logging to aid diagnosis.
        builder.Logging.AddDebug();
#endif

        // Register application services, view models and pages with the DI container.
        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterPages(builder.Services);

        var app = builder.Build();

        // Store the service provider for later use.
        Services = app.Services;

        return app;
    }

    /// <summary>
    /// Registers singleton services used by the application, including API clients and diagnostic services.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
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

    /// <summary>
    /// Registers view models with transient scope so new instances are created per page instantiation.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
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

    /// <summary>
    /// Registers each page type so it can be resolved by the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
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
