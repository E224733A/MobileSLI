using Microsoft.Maui.Storage;
namespace TourneesMobile.Services;

public sealed class SettingsService
{
    private const string ApiBaseUrlKey = "api_base_url";
    private const string LastCodeLivreurKey = "last_code_livreur";
    private const string LastNomLivreurKey = "last_nom_livreur";
    private const string LastCodeTourneeKey = "last_code_tournee";

    public string ApiBaseUrl
    {
        get => Preferences.Get(ApiBaseUrlKey, "http://10.0.2.2:5000");
        set => Preferences.Set(ApiBaseUrlKey, NormaliserBaseUrl(value));
    }

    public string LastCodeLivreur
    {
        get => Preferences.Get(LastCodeLivreurKey, "2");
        set => Preferences.Set(LastCodeLivreurKey, value.Trim());
    }

    public string LastNomLivreur
    {
        get => Preferences.Get(LastNomLivreurKey, "DAVID LEBAS");
        set => Preferences.Set(LastNomLivreurKey, value.Trim());
    }

    public string LastCodeTournee
    {
        get => Preferences.Get(LastCodeTourneeKey, "2001");
        set => Preferences.Set(LastCodeTourneeKey, value.Trim());
    }

    private static string NormaliserBaseUrl(string value)
    {
        var url = string.IsNullOrWhiteSpace(value) ? "http://10.0.2.2:5000" : value.Trim();
        return url.TrimEnd('/');
    }
}
