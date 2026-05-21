namespace MobileSLI.Services.Api;

public static class ApiTimeouts
{
    public static readonly TimeSpan HealthCheck = TimeSpan.FromSeconds(8);

    public static readonly TimeSpan ChargementTournee = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan Synchronisation = TimeSpan.FromSeconds(90);

    public static readonly TimeSpan DefaultGet = ChargementTournee;

    public static readonly TimeSpan DefaultPost = Synchronisation;

    public static readonly TimeSpan ChargementTourneeRetryDelay = TimeSpan.FromMilliseconds(1500);

    public const int ChargementTourneeRetryCount = 1;
}
