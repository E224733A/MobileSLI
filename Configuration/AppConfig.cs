namespace MobileSLI.Configuration;

public static class AppConfig
{
    /*
     * Configuration réseau finale MobileSLI.
     *
     * Mobile :
     * http://api.mobilesli.intra:5000
     *
     * Le mobile doit contacter uniquement l'API centrale.
     */
    public const string ApiBaseUrl = "http://api.mobilesli.intra:5000";

    /*
     * Références réseau documentaires.
     * Ces URL ne sont pas appelées par l'application mobile.
     */
    public const string ExpeditionWebUrl = "http://expedition.sli.local";
    public const string AdministrationWebUrl = "http://admin.sli.local";

    /*
     * Version officielle du contrat JSON mobile/API.
     */
    public const string SchemaVersion = "1.2";
}
