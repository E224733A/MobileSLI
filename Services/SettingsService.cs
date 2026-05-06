using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
namespace MobileSLI.Services;

public sealed class SettingsService
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string LastLivreurCodeKey = "last_livreur_code";

    public string DefaultApiBaseUrl => "http://192.168.1.50:5000";

    public string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, NormalizeBaseUrl(value));
    }

    public string LastLivreurCode
    {
        get => Preferences.Default.Get(LastLivreurCodeKey, string.Empty);
        set => Preferences.Default.Set(LastLivreurCodeKey, value ?? string.Empty);
    }

    public string ApplicationVersion => AppInfo.Current.VersionString;
    public string DeviceName => DeviceInfo.Current.Name;

    private static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "http://192.168.1.50:5000";
        }

        return value.Trim().TrimEnd('/');
    }
}
