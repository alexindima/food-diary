using System.IO.Compression;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcMetadataTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiscoveryAsync_ReadsPlainAndCompressedDocuments(bool compressed) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        string origin = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);
        Task server = ServeDocumentsAsync(listener, origin, compressed, timeout.Token);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityProvider(new ConfigurationBuilder().Build());
        await using ServiceProvider provider = services.BuildServiceProvider();
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("TelegramOidcMetadata");
        var manager = new ConfigurationManager<OpenIdConnectConfiguration>(origin + "/configuration",
            new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever(client) { RequireHttps = false });

        OpenIdConnectConfiguration configuration = await manager.GetConfigurationAsync(timeout.Token);

        Assert.Equal("https://oauth.telegram.org", configuration.Issuer);
        Assert.Equal("test-key", Assert.Single(configuration.JsonWebKeySet.Keys).Kid);
        await server;
    }

    private static async Task ServeDocumentsAsync(TcpListener listener, string origin, bool compressed, CancellationToken cancellationToken) {
        for (int i = 0; i < 2; i++) {
            using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken);
            await using NetworkStream stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            string? request = await reader.ReadLineAsync(cancellationToken);
            string? header;
            do {
                header = await reader.ReadLineAsync(cancellationToken);
                Assert.False(header?.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase) == true);
            } while (!string.IsNullOrEmpty(header));
            string json = request?.Contains("/configuration ", StringComparison.Ordinal) == true
                ? $$"""{"issuer":"https://oauth.telegram.org","jwks_uri":"{{origin}}/keys"}"""
                : """{"keys":[{"kty":"RSA","kid":"test-key","e":"AQAB","n":"AQAB"}]}""";
            byte[] body = Encoding.UTF8.GetBytes(json);
            if (compressed) {
                await using var buffer = new MemoryStream();
                await using (var gzip = new GZipStream(buffer, CompressionLevel.Fastest, leaveOpen: true)) {
                    await gzip.WriteAsync(body, cancellationToken);
                }
                body = buffer.ToArray();
            }
            string headers = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + body.Length.ToString(CultureInfo.InvariantCulture) + "\r\nConnection: close\r\n";
            if (compressed) {
                headers += "Content-Encoding: gzip\r\n";
            }
            await stream.WriteAsync(Encoding.ASCII.GetBytes(headers + "\r\n"), cancellationToken);
            await stream.WriteAsync(body, cancellationToken);
        }
    }
}
