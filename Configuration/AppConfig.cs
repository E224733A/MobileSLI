namespace MobileSLI.Configuration;

/// <summary>
/// Configuration applicative commune : URL API cible et version du contrat JSON mobile/API.
/// Ces valeurs sont sensibles car elles conditionnent la communication réseau et la compatibilité des payloads.
/// </summary>
public static class AppConfig
{
    /*
     * URL finale de l'API MobileSLI sur le réseau interne SLI.
     *
     * Le téléphone doit être connecté au Wi-Fi / réseau autorisé de l'entreprise.
     *
     * API HTTPS cible :
     * https://srvapi1.sli.local
     *
     * Cette URL remplace l'ancien accès HTTP temporaire :
     * http://srvapi1.sli.local:5000
     *
     * Important : le certificat serveur doit être émis pour le nom DNS
     * srvapi1.sli.local. Ne pas utiliser l'adresse IP pour le HTTPS final.
     */
    public const string ApiBaseUrl = "https://srvapi1.sli.local";

    /*
     * Version officielle du contrat JSON mobile/API.
     *
     * Le payload mobile final contient désormais la section trajet obligatoire.
     */
    public const string SchemaVersion = "1.3";

    /*
     * Exemples pour développement uniquement :
     *
     * Téléphone physique avec adb reverse HTTP local :
     * public const string ApiBaseUrl = "http://127.0.0.1:5000";
     *
     * Émulateur Android HTTP local :
     * public const string ApiBaseUrl = "http://10.0.2.2:5000";
     *
     * Secours HTTP temporaire réseau interne :
     * public const string ApiBaseUrl = "http://srvapi1.sli.local:5000";
     */
}
