using Microsoft.Maui.Networking;
namespace MobileSLI.Services;

/// <summary>
/// Provides simple utilities to check network connectivity status on the device.
/// This service wraps <see cref="Connectivity.Current"/> and exposes a boolean flag and a
/// user-friendly status text describing the current connectivity state.
/// </summary>
public sealed class ConnectivityService
{
    /// <summary>
    /// Indicates whether the device has any form of internet or local network access.
    /// </summary>
    public bool HasInternetOrLocalNetwork => Connectivity.Current.NetworkAccess != NetworkAccess.None;

    /// <summary>
    /// Returns a localized description of the current network access state.
    /// </summary>
    /// <returns>A string describing the network status in French.</returns>
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
