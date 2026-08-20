using System.Net;
using System.Net.Sockets;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public static class MailInboxLocalTlsHealthCheck {
    private const int HttpsPort = 5098;

    public static async Task<bool> IsReadyAsync(
        string? serverName,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(serverName) || Uri.CheckHostName(serverName) == UriHostNameType.Unknown) {
            return false;
        }

        using var handler = new SocketsHttpHandler {
            UseProxy = false,
            ConnectCallback = ConnectToLoopbackAsync,
        };
        using var client = new HttpClient(handler) {
            Timeout = TimeSpan.FromSeconds(4),
        };

        try {
            Uri requestUri = new UriBuilder(Uri.UriSchemeHttps, serverName, HttpsPort, "/health/ready").Uri;
            using HttpResponseMessage response = await client
                .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        } catch (HttpRequestException) {
            return false;
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return false;
        }
    }

    private static async ValueTask<Stream> ConnectToLoopbackAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken) {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try {
            await socket.ConnectAsync(IPAddress.Loopback, HttpsPort, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        } catch {
            socket.Dispose();
            throw;
        }
    }
}
