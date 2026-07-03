using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class DirectMxRelayDeliveryTransportTests {
    [Fact]
    public async Task SendAsync_WhenMxHostIsLoopbackAddress_RejectsBeforeDelivery() {
        var transport = new DirectMxRelayDeliveryTransport(
            Options.Create(new DirectMxOptions {
                Port = 25,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
            }),
            new StubMxResolver([new MxRecord("127.0.0.1", 0)]),
            CreateDkimSigningService(),
            FixedTime,
            NullLogger<DirectMxRelayDeliveryTransport>.Instance);
        var request = new RelayEmailMessageRequest(
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(request, CancellationToken.None));

        Assert.Contains("private or loopback", ex.InnerException?.Message ?? ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_WhenRecipientsSpanMultipleDomains_RejectsBeforeDelivery() {
        var transport = new DirectMxRelayDeliveryTransport(
            Options.Create(new DirectMxOptions {
                Port = 25,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
            }),
            new StubMxResolver([]),
            CreateDkimSigningService(),
            FixedTime,
            NullLogger<DirectMxRelayDeliveryTransport>.Instance);
        var request = new RelayEmailMessageRequest(
            "sender@example.com",
            "Sender",
            ["first@example.com", "second@example.net"],
            "Subject",
            "<p>Hello</p>",
            "Hello");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(request, CancellationToken.None));

        Assert.Contains("one domain", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_WhenSingleDomainMxSucceeds_SendsThroughConfiguredEndpoint() {
        var connector = new RecordingEndpointConnector();
        var smtpClient = new RecordingDirectMxSmtpClient();
        var transport = new DirectMxRelayDeliveryTransport(
            Options.Create(new DirectMxOptions {
                Port = 2525,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
                LocalDomain = "relay.example.com",
            }),
            new StubMxResolver([new MxRecord("mx.example.com", 10)]),
            CreateDkimSigningService(),
            FixedTime,
            NullLogger<DirectMxRelayDeliveryTransport>.Instance,
            connector,
            new StubDirectMxSmtpClientFactory(smtpClient));

        await transport.SendAsync(new RelayEmailMessageRequest(
            "sender@example.com",
            "Sender",
            ["first@example.com", "second@example.com"],
            "Subject",
            "<p>Hello</p>",
            TextBody: null), CancellationToken.None);

        Assert.Equal("mx.example.com", connector.Host);
        Assert.Equal(2525, connector.Port);
        Assert.Equal("relay.example.com", smtpClient.LocalDomain);
        Assert.True(smtpClient.Connected);
        Assert.True(smtpClient.Disconnected);
        Assert.NotNull(smtpClient.Message);
        Assert.Equal(2, smtpClient.Message.To.Count);
        Assert.Contains("Hello", smtpClient.Message.TextBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("172.31.255.255", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("100.127.255.255", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("8.8.8.8", true)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fec0::1", false)]
    [InlineData("ff02::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("::ffff:8.8.8.8", true)]
    public void IsPublicAddress_ReturnsExpectedResult(string value, bool expected) {
        var address = IPAddress.Parse(value);

        bool actual = DirectMxEndpointConnector.IsPublicAddress(address);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task EndpointConnector_WhenLiteralAddressIsPrivate_RejectsBeforeConnecting() {
        var connector = new DirectMxEndpointConnector();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            connector.ConnectAsync("127.0.0.1", 25, CancellationToken.None));

        Assert.Contains("private or loopback", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndpointConnector_WhenPublicAddressConnectFails_DisposesSocketAndRethrows() {
        Socket? createdSocket = null;
        string publicHost = string.Join('.', 8, 8, 8, 8);
        var connector = new DirectMxEndpointConnector(
            addressFamily => {
                createdSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                return createdSocket;
            },
            (_, _, _) => Task.FromException(new SocketException((int)SocketError.ConnectionRefused)));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            connector.ConnectAsync(publicHost, 25, CancellationToken.None));
        Assert.NotNull(createdSocket);
        Assert.Throws<ObjectDisposedException>(() => createdSocket!.NoDelay = true);
    }

    [Fact]
    public async Task EndpointConnector_WhenPublicAddressConnectSucceeds_ReturnsSocket() {
        Socket? createdSocket = null;
        IPEndPoint? connectedEndpoint = null;
        string publicHost = string.Join('.', 8, 8, 8, 8);
        var connector = new DirectMxEndpointConnector(
            addressFamily => {
                createdSocket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                return createdSocket;
            },
            (_, endpoint, _) => {
                connectedEndpoint = endpoint;
                return Task.CompletedTask;
            });

        using Socket socket = await connector.ConnectAsync(publicHost, 2525, CancellationToken.None);

        Assert.Same(createdSocket, socket);
        Assert.Equal(2525, connectedEndpoint?.Port);
    }

    [Fact]
    public void CreateMessage_WhenTextBodyIsMissing_CreatesMultipartAlternativeWithTextFallback() {
        DirectMxRelayDeliveryTransport transport = CreateTransport(CreateDkimSigningService());
        RelayEmailMessageRequest request = CreateRequest(textBody: null);

        MimeMessage message = InvokeCreateMessage(transport, request);

        Assert.Equal("Subject", message.Subject);
        Assert.Equal("sender@example.com", ((MailboxAddress)message.From.Single()).Address);
        Assert.True(message.Body is MultipartAlternative);
        Assert.Contains("Hello & welcome", message.TextBody, StringComparison.Ordinal);
        Assert.Equal("<p>Hello &amp; welcome</p>", message.HtmlBody);
        Assert.Equal(FixedNow, message.Date);
        Assert.False(message.Headers.Contains("DKIM-Signature"));
    }

    [Fact]
    public void CreateMessage_WhenDkimIsEnabled_AddsDkimSignature() {
        using var rsa = RSA.Create(1024);
        DkimSigningService dkimSigningService = CreateDkimSigningService(new MailRelayDkimOptions {
            Enabled = true,
            Domain = "example.com",
            Selector = "mail",
            PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
        });
        DirectMxRelayDeliveryTransport transport = CreateTransport(dkimSigningService);

        MimeMessage message = InvokeCreateMessage(transport, CreateRequest(textBody: "Plain text"));

        Assert.True(message.Headers.Contains("DKIM-Signature"));
        Assert.Equal("Plain text", message.TextBody);
    }

    [Fact]
    public void HtmlToText_WhenHtmlIsBlank_ReturnsEmptyString() {
        string text = InvokeHtmlToText(" ");

        Assert.Equal(string.Empty, text);
    }

    private static string InvokeHtmlToText(string html) {
        MethodInfo method = typeof(DirectMxRelayDeliveryTransport).GetMethod(
            "HtmlToText",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (string)method.Invoke(null, [html])!;
    }

    private static MimeMessage InvokeCreateMessage(
        DirectMxRelayDeliveryTransport transport,
        RelayEmailMessageRequest request) {
        MethodInfo method = typeof(DirectMxRelayDeliveryTransport).GetMethod(
            "CreateMessage",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (MimeMessage)method.Invoke(transport, [request, new[] { MailboxAddress.Parse("recipient@example.com") }])!;
    }

    private static DirectMxRelayDeliveryTransport CreateTransport(DkimSigningService dkimSigningService) =>
        new(
            Options.Create(new DirectMxOptions {
                Port = 25,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
            }),
            new StubMxResolver([]),
            dkimSigningService,
            FixedTime,
            NullLogger<DirectMxRelayDeliveryTransport>.Instance);

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static DkimSigningService CreateDkimSigningService(MailRelayDkimOptions? options = null) =>
        new(Options.Create(options ?? new MailRelayDkimOptions()), FixedTime);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    private static RelayEmailMessageRequest CreateRequest(string? textBody) =>
        new(
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello &amp; welcome</p>",
            textBody);

    [ExcludeFromCodeCoverage]
    private sealed class StubMxResolver(IReadOnlyList<MxRecord> records) : IMxResolver {
        public Task<IReadOnlyList<MxRecord>> ResolveAsync(string domain, CancellationToken cancellationToken) =>
            Task.FromResult(records);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingEndpointConnector : IDirectMxEndpointConnector {
        public string? Host { get; private set; }
        public int Port { get; private set; }

        public Task<Socket> ConnectAsync(string mxHost, int port, CancellationToken cancellationToken) {
            Host = mxHost;
            Port = port;
            return Task.FromResult(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDirectMxSmtpClientFactory(IDirectMxSmtpClient client) : IDirectMxSmtpClientFactory {
        public IDirectMxSmtpClient Create() => client;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingDirectMxSmtpClient : IDirectMxSmtpClient {
        public string LocalDomain { get; set; } = string.Empty;
        public bool Connected { get; private set; }
        public bool Disconnected { get; private set; }
        public MimeMessage? Message { get; private set; }

        public Task ConnectAsync(
            Socket socket,
            string host,
            int port,
            MailKit.Security.SecureSocketOptions options,
            CancellationToken cancellationToken) {
            socket.Dispose();
            Connected = true;
            return Task.CompletedTask;
        }

        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken) {
            Message = message;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(bool quit, CancellationToken cancellationToken) {
            Disconnected = quit;
            return Task.CompletedTask;
        }

        public void Dispose() {
        }
    }
}
