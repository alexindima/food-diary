using System.Net;
using System.Net.Sockets;

namespace FoodDiary.Integrations.Services;

internal static class WebPushSocketsHttpHandlerFactory {
    public static SocketsHttpHandler Create() {
        return new SocketsHttpHandler {
            AllowAutoRedirect = false,
            UseProxy = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = ConnectAsync,
        };
    }

    internal static bool IsPubliclyRoutable(IPAddress address) {
        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork) {
            byte[] bytes = address.GetAddressBytes();
            return bytes[0] != 0 &&
                   bytes[0] != 10 &&
                   bytes[0] != 127 &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0) &&
                   !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2) &&
                   !(bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 198 && bytes[1] is 18 or 19) &&
                   !(bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) &&
                   !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) &&
                   bytes[0] < 224;
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6 ||
            IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.IPv6None) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6Multicast ||
            address.IsIPv6SiteLocal) {
            return false;
        }

        byte[] ipv6Bytes = address.GetAddressBytes();
        bool isUniqueLocal = (ipv6Bytes[0] & 0xFE) == 0xFC;
        bool isDocumentation = ipv6Bytes[0] == 0x20 &&
                               ipv6Bytes[1] == 0x01 &&
                               ipv6Bytes[2] == 0x0D &&
                               ipv6Bytes[3] == 0xB8;
        bool isIpv4Compatible = ipv6Bytes.AsSpan(0, 12).IndexOfAnyExcept((byte)0) < 0;
        bool isNat64 = ipv6Bytes[0] == 0x00 &&
                       ipv6Bytes[1] == 0x64 &&
                       ipv6Bytes[2] == 0xFF &&
                       ipv6Bytes[3] == 0x9B &&
                       ((ipv6Bytes[4] == 0x00 && ipv6Bytes[5] == 0x01) ||
                        ipv6Bytes.AsSpan(4, 8).IndexOfAnyExcept((byte)0) < 0);
        bool isTeredo = ipv6Bytes[0] == 0x20 &&
                        ipv6Bytes[1] == 0x01 &&
                        ipv6Bytes[2] == 0x00 &&
                        ipv6Bytes[3] == 0x00;
        bool isSixToFour = ipv6Bytes[0] == 0x20 && ipv6Bytes[1] == 0x02;
        return !isUniqueLocal &&
               !isDocumentation &&
               !isIpv4Compatible &&
               !isNat64 &&
               !isTeredo &&
               !isSixToFour;
    }

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) {
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(
            context.DnsEndPoint.Host,
            cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Any(static address => !IsPubliclyRoutable(address))) {
            throw new HttpRequestException("Web push endpoint resolved to a non-public network address.");
        }

        Exception? lastException = null;
        foreach (IPAddress address in addresses) {
            cancellationToken.ThrowIfCancellationRequested();
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try {
                await socket.ConnectAsync(
                    new IPEndPoint(address, context.DnsEndPoint.Port),
                    cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            } catch (OperationCanceledException) {
                socket.Dispose();
                throw;
            } catch (Exception ex) when (ex is SocketException or IOException) {
                socket.Dispose();
                lastException = ex;
            }
        }

        throw new HttpRequestException("Unable to connect to the web push endpoint.", lastException);
    }
}
