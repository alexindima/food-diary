using System.Net;
using System.Net.Sockets;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DirectMxEndpointConnector : IDirectMxEndpointConnector {
    public async Task<Socket> ConnectAsync(string mxHost, int port, CancellationToken cancellationToken) {
        IPAddress[] addresses = IPAddress.TryParse(mxHost, out IPAddress? literalAddress)
            ? [literalAddress]
            : await Dns.GetHostAddressesAsync(mxHost, cancellationToken).ConfigureAwait(false);
        IPAddress? publicAddress = addresses.FirstOrDefault(IsPublicAddress) ?? throw new InvalidOperationException($"Direct MX host '{mxHost}' resolves only to private or loopback addresses.");
        var socket = new Socket(publicAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
            NoDelay = true,
        };

        try {
            await socket.ConnectAsync(new IPEndPoint(publicAddress, port), cancellationToken).ConfigureAwait(false);
            return socket;
        } catch {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublicAddress(IPAddress address) {
        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None)) {
            return false;
        }

        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        return address.AddressFamily == AddressFamily.InterNetwork
            ? IsPublicIpv4Address(address)
            : IsPublicIpv6Address(address);
    }

    private static bool IsPublicIpv4Address(IPAddress address) {
        byte[] bytes = address.GetAddressBytes();
        return bytes[0] != 10 &&
               bytes[0] != 127 &&
               !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
               !(bytes[0] == 192 && bytes[1] == 168) &&
               !(bytes[0] == 169 && bytes[1] == 254) &&
               !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
               bytes[0] != 0 &&
               bytes[0] < 224;
    }

    private static bool IsPublicIpv6Address(IPAddress address) {
        byte[] bytes = address.GetAddressBytes();
        return address is { IsIPv6LinkLocal: false, IsIPv6SiteLocal: false, IsIPv6Multicast: false } &&
               (bytes[0] & 0xfe) != 0xfc;
    }
}
