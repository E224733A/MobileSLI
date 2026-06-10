using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;

namespace MobileSLI.Services;

/// <summary>
/// Centralized application settings service. This class encapsulates all
/// persisted configuration values for the mobile application, such as the
/// API base URL and the last entered livreur (driver) code. It stores
/// these values using the cross‑platform <see cref="Preferences"/> API so
/// that settings survive app restarts. The service also exposes various
/// read‑only properties describing the current device (name, model,
/// manufacturer, platform, idiom, OS version, etc.) and provides helper
/// methods to normalize and reset persisted values.
/// </summary>
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
    /// <summary>
    /// Gets the default API base URL defined in <see cref="AppConfig"/>.
    /// This value is normalized to remove trailing slashes.
    /// </summary>
    public string DefaultApiBaseUrl => NormalizeBaseUrl(AppConfig.ApiBaseUrl);

    /// <summary>
    /// Gets or sets the current API base URL used for network calls. When
    /// reading, the value is retrieved from preferences and normalized to
    /// ensure it includes a scheme and no trailing slash. When setting, the
    /// provided URL is normalized before being persisted. If a legacy HTTP
    /// URL is detected and the application is now configured to use HTTPS,
    /// the property automatically migrates the stored URL to the default
    /// HTTPS value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the last livreur (driver) code entered by the user. When
    /// setting a new code the value is trimmed; when retrieving the value
    /// an empty string is returned if no code has been stored.
    /// </summary>
    public string LastLivreurCode
    {
        get => Preferences.Default.Get(LastLivreurCodeKey, string.Empty);
        set => Preferences.Default.Set(LastLivreurCodeKey, NormalizeLivreurCode(value));
    }

    /// <summary>
    /// Gets the semantic version string for the current application build.
    /// </summary>
    public string ApplicationVersion => AppInfo.Current.VersionString;

    /// <summary>
    /// Gets a user‑friendly name for the device. Falls back to the model
    /// identifier if <see cref="DeviceInfo.Current.Name"/> is blank.
    /// </summary>
    public string DeviceName => string.IsNullOrWhiteSpace(DeviceInfo.Current.Name)
        ? DeviceInfo.Current.Model
        : DeviceInfo.Current.Name;

    /// <summary>
    /// Gets the device model identifier, e.g., "Pixel 5".
    /// </summary>
    public string DeviceModel => DeviceInfo.Current.Model;

    /// <summary>
    /// Gets the device manufacturer, e.g., "Google" or "Apple".
    /// </summary>
    public string DeviceManufacturer => DeviceInfo.Current.Manufacturer;

    /// <summary>
    /// Gets the operating system platform the app is running on (Android, iOS, etc.).
    /// </summary>
    public string Platform => DeviceInfo.Current.Platform.ToString();

    /// <summary>
    /// Gets the device form factor classification (Phone, Tablet, Desktop, etc.).
    /// </summary>
    public string Idiom => DeviceInfo.Current.Idiom.ToString();

    /// <summary>
    /// Gets the OS version string for the current device.
    /// </summary>
    public string VersionString => DeviceInfo.Current.VersionString;

    /// <summary>
    /// Removes the stored API base URL from preferences so the next
    /// access reverts to the default value.
    /// </summary>
    public void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(ApiBaseUrlKey);
    }

    /// <summary>
    /// Removes the stored last livreur (driver) code from preferences.
    /// </summary>
    public void ResetLastLivreurCode()
    {
        Preferences.Default.Remove(LastLivreurCodeKey);
    }

    /// <summary>
    /// Clears all persisted settings managed by this service. Equivalent to
    /// calling <see cref="ResetApiBaseUrl"/> and <see cref="ResetLastLivreurCode"/>.
    /// </summary>
    public void ResetAll()
    {
        ResetApiBaseUrl();
        ResetLastLivreurCode();
    }

    /// <summary>
    /// Determines whether a stored base URL is the legacy HTTP endpoint that
    /// should be migrated to the new default HTTPS endpoint. Returns
    /// <c>true</c> when the normalized URL exactly matches the legacy URL and
    /// the default API base URL uses HTTPS; otherwise <c>false</c>.
    /// </summary>
    private static bool ShouldMigrateLegacyHttpUrl(string normalizedUrl)
    {
        return string.Equals(normalizedUrl, LegacyHttpApiBaseUrl, StringComparison.OrdinalIgnoreCase)
            && AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a base URL by trimming whitespace, removing any trailing slash
    /// and ensuring a scheme (HTTP/HTTPS) prefix. If the input is null or
    /// whitespace, the default API base URL is returned. Otherwise the value
    /// is prepended with the default scheme when it lacks one.
    /// </summary>
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

    /// <summary>
    /// Returns the default URL scheme ("https://" or "http://") based on the
    /// current API base URL in <see cref="AppConfig"/>. If the default API
    /// base URL starts with "https://", that scheme is returned; otherwise
    /// "http://" is returned.
    /// </summary>
    private static string GetDefaultScheme()
    {
        return AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "https://"
            : "http://";
    }

    /// <summary>
    /// Normalizes the livreur (driver) code by trimming whitespace and
    /// returning an empty string when null or whitespace.
    /// </summary>
    private static string NormalizeLivreurCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
