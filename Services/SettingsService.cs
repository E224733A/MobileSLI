using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;

namespace MobileSLI.Services;

public sealed class SettingsService
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string LastLivreurCodeKey = "last_livreur_code";
    private const string AllowedApiHost = "api.mobilesli.intra";

    public string DefaultApiBaseUrl => NormalizeBaseUrl(AppConfig.ApiBaseUrl);

    public string ApiBaseUrl
    {
        get
        {
            var savedValue = Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
            var normalized = NormalizeBaseUrl(savedValue);

            if (!IsAllowedApiEndpoint(normalized))
            {
                Preferences.Default.Set(ApiBaseUrlKey, DefaultApiBaseUrl);
                return DefaultApiBaseUrl;
            }

            return normalized;
        }
        set
        {
            var normalized = NormalizeBaseUrl(value);
            Preferences.Default.Set(
                ApiBaseUrlKey,
                IsAllowedApiEndpoint(normalized) ? normalized : DefaultApiBaseUrl);
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

    private static bool IsAllowedApiEndpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Host, AllowedApiHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return uri.Port == 5000;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return uri.Port is 443 or 5000;
        }

        return false;
    }

    private static string NormalizeLivreurCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
