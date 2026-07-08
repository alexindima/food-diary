using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class SmtpRelayDeliveryTransportTests {
    [Fact]
    public async Task SendAsync_SendsMimeMessageThroughSmtpServerAndAuthenticatesWhenConfigured() {
        await using var server = new RecordingSmtpServer();
        await server.StartAsync();
        var transport = new SmtpRelayDeliveryTransport(
            Options.Create(new MailRelaySmtpOptions {
                Host = IPAddress.Loopback.ToString(),
                Port = server.Port,
                UseSsl = false,
                User = "relay-user",
                Password = "relay-password",
            }),
            CreateDkimSigningService(),
            FixedTime);

        await transport.SendAsync(new RelayEmailMessageRequest(
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello <strong>world</strong></p>",
            TextBody: null), CancellationToken.None);

        string data = await server.WaitForMessageDataAsync();
        Assert.True(server.Authenticated);
        Assert.Contains("From: Sender <sender@example.com>", data, StringComparison.Ordinal);
        Assert.Contains("To: recipient@example.com", data, StringComparison.Ordinal);
        Assert.Contains("Subject: Subject", data, StringComparison.Ordinal);
        Assert.Contains("Date: Wed, 01 Jul 2026", data, StringComparison.Ordinal);
        Assert.Contains("Hello", data, StringComparison.Ordinal);
        Assert.Contains("world", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_WhenDkimIsEnabled_AddsDkimSignature() {
        await using var server = new RecordingSmtpServer();
        await server.StartAsync();
        using var rsa = RSA.Create(1024);
        var transport = new SmtpRelayDeliveryTransport(
            Options.Create(new MailRelaySmtpOptions {
                Host = IPAddress.Loopback.ToString(),
                Port = server.Port,
                UseSsl = false,
            }),
            CreateDkimSigningService(new MailRelayDkimOptions {
                Enabled = true,
                Domain = "example.com",
                Selector = "mail",
                PrivateKeyPem = rsa.ExportPkcs8PrivateKeyPem(),
            }),
            FixedTime);

        await transport.SendAsync(CreateRequest(textBody: "Hello"), CancellationToken.None);

        string data = await server.WaitForMessageDataAsync();
        Assert.False(server.Authenticated);
        Assert.Contains("DKIM-Signature:", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_WhenHtmlAndTextAreBlank_SendsEmptyTextBodyWithoutAuthentication() {
        await using var server = new RecordingSmtpServer();
        await server.StartAsync();
        var transport = new SmtpRelayDeliveryTransport(
            Options.Create(new MailRelaySmtpOptions {
                Host = IPAddress.Loopback.ToString(),
                Port = server.Port,
                UseSsl = false,
            }),
            CreateDkimSigningService(),
            FixedTime);

        await transport.SendAsync(CreateRequest(htmlBody: " ", textBody: null), CancellationToken.None);

        await server.WaitForMessageDataAsync();
        Assert.False(server.Authenticated);
    }

    [Fact]
    public async Task ConfigurableTransport_WhenModeIsSmtp_UsesSmtpTransport() {
        await using var server = new RecordingSmtpServer();
        await server.StartAsync();
        var smtpTransport = new SmtpRelayDeliveryTransport(
            Options.Create(new MailRelaySmtpOptions {
                Host = IPAddress.Loopback.ToString(),
                Port = server.Port,
                UseSsl = false,
            }),
            CreateDkimSigningService(),
            FixedTime);
        var transport = new ConfigurableRelayDeliveryTransport(
            smtpTransport,
            CreateDirectMxTransport("127.0.0.1"),
            Options.Create(new MailRelayDeliveryOptions {
                Mode = MailRelayDeliveryOptions.SmtpSubmissionMode,
            }));

        await transport.SendAsync(CreateRequest(textBody: "Hello"), CancellationToken.None);

        string data = await server.WaitForMessageDataAsync();
        Assert.Contains("Subject: Subject", data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfigurableTransport_WhenModeIsDirectMx_UsesDirectMxTransport() {
        var smtpTransport = new SmtpRelayDeliveryTransport(
            Options.Create(new MailRelaySmtpOptions {
                Host = "invalid.local",
                Port = 25,
                UseSsl = false,
            }),
            CreateDkimSigningService(),
            FixedTime);
        var transport = new ConfigurableRelayDeliveryTransport(
            smtpTransport,
            CreateDirectMxTransport("127.0.0.1"),
            Options.Create(new MailRelayDeliveryOptions {
                Mode = MailRelayDeliveryOptions.DirectMxMode,
            }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(CreateRequest(textBody: "Hello"), CancellationToken.None));

        Assert.Contains("private or loopback", ex.InnerException?.Message ?? ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfigurableTransport_WhenModeIsUnsupported_ThrowsConfigurationError() {
        var transport = new ConfigurableRelayDeliveryTransport(
            new SmtpRelayDeliveryTransport(
                Options.Create(new MailRelaySmtpOptions()),
                CreateDkimSigningService(),
                FixedTime),
            CreateDirectMxTransport("127.0.0.1"),
            Options.Create(new MailRelayDeliveryOptions {
                Mode = "unsupported",
            }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            transport.SendAsync(CreateRequest(textBody: "Hello"), CancellationToken.None));

        Assert.Contains("Unsupported mail relay delivery mode", ex.Message, StringComparison.Ordinal);
    }

    private static RelayEmailMessageRequest CreateRequest(string htmlBody = "<p>Hello</p>", string? textBody = null) =>
        new(
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            htmlBody,
            textBody);

    private static DirectMxRelayDeliveryTransport CreateDirectMxTransport(string mxHost) =>
        new(
            Options.Create(new DirectMxOptions {
                Port = 25,
                ConnectTimeoutSeconds = 1,
                UseStartTlsWhenAvailable = false,
            }),
            new StubMxResolver([new MxRecord(mxHost, 0)]),
            CreateDkimSigningService(),
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

    [ExcludeFromCodeCoverage]
    private sealed class StubMxResolver(IReadOnlyList<MxRecord> records) : IMxResolver {
        public Task<IReadOnlyList<MxRecord>> ResolveAsync(string domain, CancellationToken cancellationToken) =>
            Task.FromResult(records);
    }

    private sealed class RecordingSmtpServer : IAsyncDisposable {
        private readonly TcpListener _listener = new(IPAddress.Loopback, port: 0);
        private readonly TaskCompletionSource<string> _messageData = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _serverTask;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public bool Authenticated { get; private set; }

        public Task StartAsync() {
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            _listener.Start();
            _serverTask = RunAsync(_cancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task<string> WaitForMessageDataAsync() {
            Task completed = await Task.WhenAny(_messageData.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
            if (!ReferenceEquals(completed, _messageData.Task)) {
                throw new TimeoutException("SMTP test server did not receive message data.");
            }

            return await _messageData.Task.ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync() {
            if (_cancellationTokenSource is not null) {
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _listener.Stop();

            if (_serverTask is not null) {
                try {
                    await _serverTask.ConfigureAwait(false);
                } catch (OperationCanceledException) {
                } catch (SocketException) {
                } catch (ObjectDisposedException) {
                }
            }

            _cancellationTokenSource?.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken) {
            using TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            NetworkStream stream = client.GetStream();
            await using (stream.ConfigureAwait(false)) {
                using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) {
                    NewLine = "\r\n",
                    AutoFlush = true,
                };
                await using (writer.ConfigureAwait(false)) {

                    await writer.WriteLineAsync("220 localhost ESMTP").ConfigureAwait(false);
                    while (!cancellationToken.IsCancellationRequested) {
                        string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (line is null) {
                            return;
                        }

                        if (line.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("HELO ", StringComparison.OrdinalIgnoreCase)) {
                            await writer.WriteLineAsync("250-localhost").ConfigureAwait(false);
                            await writer.WriteLineAsync("250-AUTH PLAIN").ConfigureAwait(false);
                            await writer.WriteLineAsync("250 OK").ConfigureAwait(false);
                        } else if (line.StartsWith("AUTH PLAIN", StringComparison.OrdinalIgnoreCase)) {
                            Authenticated = true;
                            await writer.WriteLineAsync("235 2.7.0 Authentication successful").ConfigureAwait(false);
                        } else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase)) {
                            await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>").ConfigureAwait(false);
                            _messageData.TrySetResult(await ReadMessageDataAsync(reader, cancellationToken).ConfigureAwait(false));
                            await writer.WriteLineAsync("250 2.0.0 Message accepted").ConfigureAwait(false);
                        } else if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase)) {
                            await writer.WriteLineAsync("221 2.0.0 Bye").ConfigureAwait(false);
                            return;
                        } else {
                            await writer.WriteLineAsync("250 OK").ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private static async Task<string> ReadMessageDataAsync(
            StreamReader reader,
            CancellationToken cancellationToken) {
            var builder = new StringBuilder();
            while (true) {
                string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line?.Equals(".", StringComparison.Ordinal) != false) {
                    return builder.ToString();
                }

                builder.AppendLine(line);
            }
        }
    }
}
