using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;

namespace MobileSLI.Services;

public sealed class SettingsService
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string LastLivreurCodeKey = "last_livreur_code";

    /*
     * Ancienne URL de production HTTP utilisée avant la préparation HTTPS.
     *
     * Objectif : éviter qu'un téléphone déjà installé continue à appeler
     * automatiquement l'ancien endpoint stocké dans Preferences après mise à jour.
     *
     * Si un test HTTP de secours est nécessaire, il reste possible de ressaisir
     * explicitement http://srvapi1.sli.local:5000 dans l'application.
     */
    private const string LegacyHttpApiBaseUrl = AppConfig.ApiBaseUrl;

    /*
     * L'URL par défaut est centralisée dans :
     *
     * Configuration/AppConfig.cs
     *
     * URL cible HTTPS :
     * https://srvapi1.sli.local
     *
     * La CA publique Android doit être présente localement dans :
     * Platforms/Android/Resources/raw/mobilesli_root_ca.crt
     */
    public string DefaultApiBaseUrl => NormalizeBaseUrl(AppConfig.ApiBaseUrl);

    public string ApiBaseUrl
    {
        get
        {
            var savedValue = Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
            var normalized = NormalizeBaseUrl(savedValue);

            if (ShouldMigrateLegacyHttpUrl(normalized))
            {
                Preferences.Default.Set(ApiBaseUrlKey, DefaultApiBaseUrl);
                return DefaultApiBaseUrl;
            }

            return normalized;
        }
        set
        {
            Preferences.Default.Set(ApiBaseUrlKey, NormalizeBaseUrl(value));
        }
    }

    public string LastLivreurCode
    {
        get => Preferences.Default.Get(LastLivreurCodeKey, string.Empty);
        set => Preferences.Default.Set(LastLivreurCodeKey, NormalizeLivreurCode(value));
    }

    public string ApplicationVersion => AppInfo.Current.VersionString;

    public string DeviceName => string.IsNullOrWhiteSpace(DeviceInfo.Current.Name)
        ? DeviceInfo.Current.Model
        : DeviceInfo.Current.Name;

    public string DeviceModel => DeviceInfo.Current.Model;

    public string DeviceManufacturer => DeviceInfo.Current.Manufacturer;

    public string Platform => DeviceInfo.Current.Platform.ToString();

    public string Idiom => DeviceInfo.Current.Idiom.ToString();

    public string VersionString => DeviceInfo.Current.VersionString;

    public void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(ApiBaseUrlKey);
    }

    public void ResetLastLivreurCode()
    {
        Preferences.Default.Remove(LastLivreurCodeKey);
    }

    public void ResetAll()
    {
        ResetApiBaseUrl();
        ResetLastLivreurCode();
    }

    private static bool ShouldMigrateLegacyHttpUrl(string normalizedUrl)
    {
        return string.Equals(normalizedUrl, LegacyHttpApiBaseUrl, StringComparison.OrdinalIgnoreCase)
            && AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AppConfig.ApiBaseUrl.Trim().TrimEnd('/');
        }

        var normalized = value.Trim().TrimEnd('/');

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = GetDefaultScheme() + normalized;
        }

        return normalized;
    }

    private static string GetDefaultScheme()
    {
        return AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "https://"
            : "http://";
    }

    private static string NormalizeLivreurCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
