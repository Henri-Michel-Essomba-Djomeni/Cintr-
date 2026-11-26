using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Cintre.App.Helpers;

public static class ReseauHelper
{
    // Adresses IPv4 de la machine (l'adresse Tailscale 100.x.y.z vient en premier)
    public static List<string> ObtenirAdressesIp()
    {
        var adresses = new List<string>();

        foreach (var carte in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (carte.OperationalStatus != OperationalStatus.Up ||
                carte.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var ua in carte.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    adresses.Add(ua.Address.ToString());
            }
        }

        return adresses
            .Distinct()
            .OrderByDescending(EstAdresseTailscale)
            .ThenBy(a => a)
            .ToList();
    }

    // Tailscale utilise la plage 100.64.0.0/10
    public static bool EstAdresseTailscale(string ip)
    {
        if (!IPAddress.TryParse(ip, out var adresse)) return false;
        var o = adresse.GetAddressBytes();
        return o.Length == 4 && o[0] == 100 && o[1] >= 64 && o[1] <= 127;
    }
}