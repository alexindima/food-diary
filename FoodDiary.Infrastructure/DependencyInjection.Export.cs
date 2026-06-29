using System.Net;
using System.Net.Sockets;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Infrastructure.Services.DiaryPdf;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    internal static Func<string, CancellationToken, ValueTask<IPAddress[]>> ResolveRemoteImageHostAddressesAsync { get; set; } =
        static async (host, cancellationToken) => await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);

    internal static Func<IPAddress, int, CancellationToken, ValueTask<Stream>> ConnectRemoteImageSocketAsync { get; set; } =
        ConnectRemoteImageSocketCoreAsync;

    private static IServiceCollection AddExportInfrastructure(this IServiceCollection services) {
        services.AddHttpClient<IDiaryPdfGenerator, DiaryPdfGenerator>(client => { client.Timeout = TimeSpan.FromSeconds(5); })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                AllowAutoRedirect = false,
                ConnectCallback = ConnectToAllowedRemoteImageEndpointAsync,
            });

        return services;
    }

    private static async ValueTask<Stream> ConnectToAllowedRemoteImageEndpointAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) {
        IPAddress[] addresses = await ResolveRemoteImageHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
        IPAddress publicAddress = addresses.FirstOrDefault(IsPublicAddress) ?? throw new HttpRequestException("Remote image host resolves only to private or loopback addresses.");
        return await ConnectRemoteImageSocketAsync(publicAddress, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<Stream> ConnectRemoteImageSocketCoreAsync(
        IPAddress publicAddress,
        int port,
        CancellationToken cancellationToken) {
        var socket = new Socket(publicAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
            NoDelay = true,
        };

        try {
            await socket.ConnectAsync(new IPEndPoint(publicAddress, port), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        } catch {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublicAddress(IPAddress address) {
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

        return IsPublicAddressCore(
            address.AddressFamily,
            address.GetAddressBytes(),
            address.IsIPv6LinkLocal,
            address.IsIPv6SiteLocal,
            address.IsIPv6Multicast);
    }

    internal static bool IsPublicAddressCore(
        AddressFamily addressFamily,
        byte[] bytes,
        bool isIPv6LinkLocal,
        bool isIPv6SiteLocal,
        bool isIPv6Multicast) {
        switch (addressFamily) {
            case AddressFamily.InterNetwork:
                return bytes[0] != 10 &&
                       bytes[0] != 127 &&
                       !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                       !(bytes[0] == 192 && bytes[1] == 168) &&
                       !(bytes[0] == 169 && bytes[1] == 254) &&
                       !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                       bytes[0] != 0 &&
                       !(bytes[0] >= 224);
            case AddressFamily.InterNetworkV6:
                return !isIPv6LinkLocal &&
                       !isIPv6SiteLocal &&
                       !isIPv6Multicast &&
                       (bytes[0] & 0xfe) != 0xfc;
            default:
                return false;
        }
    }
}
