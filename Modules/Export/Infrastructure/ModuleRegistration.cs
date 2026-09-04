using System.Net;
using System.Net.Sockets;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Modules.Export.Infrastructure.Resources;
using FoodDiary.Modules.Export.Infrastructure.Services.DiaryPdf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Export.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddExportResources(this IServiceCollection services) =>
        services.AddSingleton<IDiaryPdfReportTextProvider, DiaryPdfReportResourceTextProvider>();

    internal static Func<string, CancellationToken, ValueTask<IPAddress[]>> ResolveRemoteImageHostAddressesAsync { get; set; } =
        static async (host, cancellationToken) => await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);

    internal static Func<IPAddress, int, CancellationToken, ValueTask<Stream>> ConnectRemoteImageSocketAsync { get; set; } =
        ConnectRemoteImageSocketCoreAsync;

    public static IServiceCollection AddExportInfrastructure(this IServiceCollection services) {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<IDiaryPdfGenerator, DiaryPdfGenerator>(client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                AllowAutoRedirect = false,
                ConnectCallback = ConnectToAllowedRemoteImageEndpointAsync,
                UseProxy = false,
            });

        return services;
    }

    private static async ValueTask<Stream> ConnectToAllowedRemoteImageEndpointAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) {
        IPAddress[] addresses = await ResolveRemoteImageHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
        IPAddress publicAddress = addresses.FirstOrDefault(RemoteImageAddressPolicy.IsPublicAddress) ?? throw new HttpRequestException("Remote image host resolves only to private or loopback addresses.");
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

    private static bool IsPublicAddress(IPAddress address) => RemoteImageAddressPolicy.IsPublicAddress(address);

    internal static bool IsPublicAddressCore(
        AddressFamily addressFamily,
        byte[] bytes,
        bool isIPv6LinkLocal,
        bool isIPv6SiteLocal,
        bool isIPv6Multicast) =>
        RemoteImageAddressPolicy.IsPublicAddressCore(
            addressFamily,
            bytes,
            isIPv6LinkLocal,
            isIPv6SiteLocal,
            isIPv6Multicast);
}
