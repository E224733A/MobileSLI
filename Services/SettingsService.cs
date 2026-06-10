using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;

namespace MobileSLI.Services;

/// <summary>
/// Service centralisé des paramètres persistés et des informations appareil.
/// Il gère notamment l'URL API utilisée par le mobile, le dernier code livreur saisi
/// et les métadonnées envoyées dans le payload de synchronisation.
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
    /// URL API par défaut normalisée depuis AppConfig.
    /// </summary>
    public string DefaultApiBaseUrl => NormalizeBaseUrl(AppConfig.ApiBaseUrl);

    /// <summary>
    /// URL API réellement utilisée par les appels réseau.
    /// La valeur est persistée dans Preferences, normalisée à la lecture et migrée automatiquement
    /// vers HTTPS si une ancienne URL HTTP connue est encore stockée sur le téléphone.
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
    /// Dernier code livreur saisi, conservé pour faciliter la reprise lors des ouvertures suivantes.
    /// </summary>
    public string LastLivreurCode
    {
        get => Preferences.Default.Get(LastLivreurCodeKey, string.Empty);
        set => Preferences.Default.Set(LastLivreurCodeKey, NormalizeLivreurCode(value));
    }

    /// <summary>
    /// Version applicative envoyée dans les informations techniques de synchronisation.
    /// </summary>
    public string ApplicationVersion => AppInfo.Current.VersionString;

    /// <summary>
    /// Nom de l'appareil envoyé dans le payload mobile, avec repli sur le modèle si le nom est vide.
    /// </summary>
    public string DeviceName => string.IsNullOrWhiteSpace(DeviceInfo.Current.Name)
        ? DeviceInfo.Current.Model
        : DeviceInfo.Current.Name;

    public string DeviceModel => DeviceInfo.Current.Model;

    public string DeviceManufacturer => DeviceInfo.Current.Manufacturer;

    public string Platform => DeviceInfo.Current.Platform.ToString();

    public string Idiom => DeviceInfo.Current.Idiom.ToString();

    public string VersionString => DeviceInfo.Current.VersionString;

    /// <summary>
    /// Supprime l'URL API stockée pour revenir à l'URL par défaut au prochain accès.
    /// </summary>
    public void ResetApiBaseUrl()
    {
        Preferences.Default.Remove(ApiBaseUrlKey);
    }

    /// <summary>
    /// Supprime le dernier code livreur mémorisé localement.
    /// </summary>
    public void ResetLastLivreurCode()
    {
        Preferences.Default.Remove(LastLivreurCodeKey);
    }

    /// <summary>
    /// Réinitialise les paramètres persistés gérés par ce service.
    /// </summary>
    public void ResetAll()
    {
        ResetApiBaseUrl();
        ResetLastLivreurCode();
    }

    /// <summary>
    /// Détecte si l'URL stockée correspond à l'ancien endpoint HTTP devant être remplacé par l'URL HTTPS par défaut.
    /// </summary>
    private static bool ShouldMigrateLegacyHttpUrl(string normalizedUrl)
    {
        return string.Equals(normalizedUrl, LegacyHttpApiBaseUrl, StringComparison.OrdinalIgnoreCase)
            && AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalise une URL API : suppression des espaces, suppression du slash final
    /// et ajout du schéma HTTP/HTTPS par défaut si l'utilisateur ne l'a pas saisi.
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
    /// Retourne le schéma réseau cohérent avec l'URL API par défaut.
    /// Cette règle évite de transformer accidentellement une URL HTTPS en HTTP.
    /// </summary>
    private static string GetDefaultScheme()
    {
        return AppConfig.ApiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "https://"
            : "http://";
    }

    /// <summary>
    /// Normalise le code livreur avant stockage local.
    /// </summary>
    private static string NormalizeLivreurCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }
}
