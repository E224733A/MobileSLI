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
     * L'URL par défaut est centralisée dans :
     *
     * Configuration/AppConfig.cs
     *
     * Mode actuel téléphone physique + adb reverse :
     * http://127.0.0.1:5000
     *
     * Commande à lancer avant le test :
     * adb reverse tcp:5000 tcp:5000
     */
    public string DefaultApiBaseUrl => NormalizeBaseUrl(AppConfig.ApiBaseUrl);

    public string ApiBaseUrl
    {
        get
        {
            var savedValue = Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
            return NormalizeBaseUrl(savedValue);
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
            normalized = "http://" + normalized;
        }

        return normalized;
    }

    private static string NormalizeLivreurCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}