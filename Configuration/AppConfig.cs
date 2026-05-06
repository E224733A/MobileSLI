namespace MobileSLI.Configuration;

public static class AppConfig
{
    /*
     * Mode téléphone physique avec adb reverse :
     *
     * Commande à lancer avant le test :
     * adb reverse tcp:5000 tcp:5000
     *
     * URL dans l'application :
     * http://127.0.0.1:5000
     */
    public const string ApiBaseUrl = "http://127.0.0.1:5000";

    /*
     * Exemples pour plus tard :
     *
     * Téléphone sans adb reverse, accès direct au PC ou à la VM :
     * public const string ApiBaseUrl = "http://192.168.1.66:5000";
     *
     * Émulateur Android :
     * public const string ApiBaseUrl = "http://10.0.2.2:5000";
     *
     * API installée sur une VM avec nom DNS interne :
     * public const string ApiBaseUrl = "http://api-mobile-sli.local:5000";
     */
}