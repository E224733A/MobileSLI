using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TourneesMobile.Services;
using TourneesMobile.ViewModels;
using TourneesMobile.Pages;

namespace TourneesMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<DemoDataService>();

        builder.Services.AddTransient<TourneeJourViewModel>();
        builder.Services.AddTransient<ChargementViewModel>();
        builder.Services.AddTransient<ListeArretsViewModel>();
        builder.Services.AddTransient<DetailArretViewModel>();
        builder.Services.AddTransient<FinTourneeViewModel>();
        builder.Services.AddTransient<SynchronisationResultViewModel>();

        builder.Services.AddTransient<TourneeJourPage>();
        builder.Services.AddTransient<ChargementPage>();
        builder.Services.AddTransient<ListeArretsPage>();
        builder.Services.AddTransient<DetailArretPage>();
        builder.Services.AddTransient<FinTourneePage>();
        builder.Services.AddTransient<SynchronisationResultPage>();

        var app = builder.Build();
        AppServices.Configure(app.Services);
        return app;
    }
}

public static class AppServices
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static void Configure(IServiceProvider services)
    {
        Services = services;
    }

    public static T Get<T>() where T : notnull => Services.GetRequiredService<T>();
}
