using System.Net;
using System.Net.Sockets;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DirectMxEndpointConnector(
    Func<AddressFamily, Socket>? socketFactory = null,
    Func<Socket, IPEndPoint, CancellationToken, Task>? connectAsync = null) : IDirectMxEndpointConnector {
    private readonly Func<AddressFamily, Socket> _socketFactory =
        socketFactory ?? (static addressFamily => new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp));
    private readonly Func<Socket, IPEndPoint, CancellationToken, Task> _connectAsync =
        connectAsync ?? (static (socket, endpoint, cancellationToken) => socket.ConnectAsync(endpoint, cancellationToken).AsTask());

    public async Task<Socket> ConnectAsync(string mxHost, int port, CancellationToken cancellationToken) {
        IPAddress[] addresses = IPAddress.TryParse(mxHost, out IPAddress? literalAddress)
            ? [literalAddress]
            : await Dns.GetHostAddressesAsync(mxHost, cancellationToken).ConfigureAwait(false);
        IPAddress? publicAddress = addresses.FirstOrDefault(IsPublicAddress) ?? throw new InvalidOperationException($"Direct MX host '{mxHost}' resolves only to private or loopback addresses.");
        Socket socket = _socketFactory(publicAddress.AddressFamily);
        socket.NoDelay = true;

        try {
            await _connectAsync(socket, new IPEndPoint(publicAddress, port), cancellationToken).ConfigureAwait(false);
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
        if (bytes[0] is 0 or 10) {
            return false;
        }

        if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) {
            return false;
        }

        if (bytes[0] == 192 && bytes[1] == 168) {
            return false;
        }

        if (bytes[0] == 169 && bytes[1] == 254) {
            return false;
        }

        if (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) {
            return false;
        }

        return bytes[0] < 224;
    }

    private static bool IsPublicIpv6Address(IPAddress address) {
        byte[] bytes = address.GetAddressBytes();
        return address is { IsIPv6LinkLocal: false, IsIPv6SiteLocal: false, IsIPv6Multicast: false } &&
               (bytes[0] & 0xfe) != 0xfc;
    }
}
