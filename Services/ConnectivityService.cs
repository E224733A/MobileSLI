using Microsoft.Maui.Networking;
namespace MobileSLI.Services;

/// <summary>
/// Service de lecture de l'état réseau du téléphone.
/// Il encapsule Connectivity.Current pour fournir un booléen simple et un message affichable côté interface.
/// </summary>
public sealed class ConnectivityService
{
    /// <summary>
    /// Indique si le téléphone dispose d'un accès réseau quelconque, Internet ou réseau local.
    /// </summary>
    public bool HasInternetOrLocalNetwork => Connectivity.Current.NetworkAccess != NetworkAccess.None;

    /// <summary>
    /// Retourne un libellé utilisateur correspondant à l'état réseau courant.
    /// </summary>
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
