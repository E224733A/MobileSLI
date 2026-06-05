namespace MobileSLI.Configuration;

public static class AppConfig
{
    /*
     * URL finale de l'API MobileSLI sur le réseau interne SLI.
     *
     * Le téléphone doit être connecté au Wi-Fi / réseau autorisé de l'entreprise.
     *
     * API :
     * http://srvapi1.sli.local:5000
     *
     * Cette URL remplace le mode de test local avec adb reverse :
     * http://127.0.0.1:5000
     */
    public const string ApiBaseUrl = "http://srvapi1.sli.local:5000";

    /*
     * Version officielle du contrat JSON mobile/API.
     *
     * Le passage à 1.3 est fait uniquement après validation API serveur 1.3.
     * Le payload mobile final contient désormais la section trajet obligatoire.
     */
    public const string SchemaVersion = "1.3";

    /*
     * Exemples pour développement uniquement :
     *
     * Téléphone physique avec adb reverse :
     * public const string ApiBaseUrl = "http://127.0.0.1:5000";
     *
     * Émulateur Android :
     * public const string ApiBaseUrl = "http://10.0.2.2:5000";
     *
     * Accès direct temporaire par IP :
     * public const string ApiBaseUrl = "http://192.168.1.233:5000";
     */
}
