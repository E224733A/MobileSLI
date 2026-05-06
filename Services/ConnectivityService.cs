using Microsoft.Maui.Networking;
namespace MobileSLI.Services;

public sealed class ConnectivityService
{
    public bool HasInternetOrLocalNetwork => Connectivity.Current.NetworkAccess != NetworkAccess.None;

    public string GetNetworkStatusText()
    {
        return Connectivity.Current.NetworkAccess switch
        {
            NetworkAccess.Internet => "Réseau disponible",
            NetworkAccess.Local => "Réseau local disponible",
            NetworkAccess.ConstrainedInternet => "Réseau limité",
            NetworkAccess.None => "Aucun réseau détecté",
            _ => "État réseau inconnu"
        };
    }
}
