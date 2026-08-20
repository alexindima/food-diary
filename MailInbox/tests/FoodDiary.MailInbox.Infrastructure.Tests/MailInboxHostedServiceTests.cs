using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxHostedServiceTests {
    [Fact]
    public async Task SmtpHostedService_WhenDisabled_CompletesWithoutStartingListener() {
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions { Enabled = false });

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SmtpHostedService_ExecuteAsync_WhenDisabled_ReturnsWithoutStartingListener() {
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions { Enabled = false });

        await InvokeExecuteAsync(service, CancellationToken.None);
    }

    [Fact]
    public async Task SmtpHostedService_WhenEnabled_AdvertisesAndNegotiatesStartTls() {
        int port = GetFreeTcpPort();
        using CertificateFiles certificateFiles = CreateCertificateFiles("localhost");
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            ListenAddress = System.Net.IPAddress.Loopback.ToString(),
            Port = port,
            CertificatePath = certificateFiles.CertificatePath,
            PrivateKeyPath = certificateFiles.PrivateKeyPath,
            MaxMessageSizeBytes = 1024,
        });

        await service.StartAsync(CancellationToken.None);
        try {
            await WaitForPortAsync(port, CancellationToken.None);
            using var client = new TcpClient();
            await client.ConnectAsync(System.Net.IPAddress.Loopback, port, CancellationToken.None);
            using (var reader = new StreamReader(
                       client.GetStream(),
                       Encoding.ASCII,
                       detectEncodingFromByteOrderMarks: false,
                       leaveOpen: true))
            await using (var writer = new StreamWriter(
                       client.GetStream(),
                       Encoding.ASCII,
                       leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" }) {
                Assert.StartsWith(
                    "220",
                    await reader.ReadLineAsync(CancellationToken.None),
                    StringComparison.Ordinal);

                await writer.WriteLineAsync("EHLO localhost");
                IReadOnlyList<string> capabilities = await ReadSmtpResponseAsync(reader, "250");
                Assert.Contains(capabilities, static line => line.Contains("STARTTLS", StringComparison.Ordinal));

                await writer.WriteLineAsync("STARTTLS");
                Assert.StartsWith(
                    "220",
                    await reader.ReadLineAsync(CancellationToken.None),
                    StringComparison.Ordinal);
            }

            X509Certificate2? remoteCertificate = null;
            await using var secureStream = new SslStream(
                client.GetStream(),
                leaveInnerStreamOpen: false,
                (_, certificate, _, _) => {
                    remoteCertificate = certificate is null
                        ? null
                        : X509CertificateLoader.LoadCertificate(certificate.GetRawCertData());
                    return certificate is not null;
                });
            await secureStream.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions {
                    TargetHost = "localhost",
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                },
                CancellationToken.None);

            using (remoteCertificate) {
                Assert.True(secureStream.IsAuthenticated);
                Assert.True(secureStream.IsEncrypted);
                Assert.Contains(secureStream.SslProtocol, new[] { SslProtocols.Tls12, SslProtocols.Tls13 });
                Assert.NotNull(remoteCertificate);
                Assert.Equal(certificateFiles.Thumbprint, remoteCertificate.Thumbprint);
            }
        } finally {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SmtpHostedService_WhenCertificateDoesNotMatchServerName_FailsClosed() {
        using CertificateFiles certificateFiles = CreateCertificateFiles("other.example");
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            Port = GetFreeTcpPort(),
            CertificatePath = certificateFiles.CertificatePath,
            PrivateKeyPath = certificateFiles.PrivateKeyPath,
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeExecuteAsync(service, CancellationToken.None));

        Assert.Contains("does not match", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SmtpHostedService_WhenCertificateFilesAreMissing_FailsClosed() {
        string missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.pem");
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            Port = GetFreeTcpPort(),
            CertificatePath = missingPath,
            PrivateKeyPath = missingPath,
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeExecuteAsync(service, CancellationToken.None));

        Assert.Contains("could not be loaded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SmtpHostedService_WhenCertificateIsExpired_FailsClosed() {
        using CertificateFiles certificateFiles = CreateCertificateFiles(
            "localhost",
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddDays(-1));
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            Port = GetFreeTcpPort(),
            CertificatePath = certificateFiles.CertificatePath,
            PrivateKeyPath = certificateFiles.PrivateKeyPath,
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeExecuteAsync(service, CancellationToken.None));

        Assert.Contains("not currently valid", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SmtpHostedService_WhenGlobalConnectionLimitIsReached_DelaysNextSessionUntilRelease() {
        int port = GetFreeTcpPort();
        using CertificateFiles certificateFiles = CreateCertificateFiles("localhost");
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            ListenAddress = System.Net.IPAddress.Loopback.ToString(),
            Port = port,
            CertificatePath = certificateFiles.CertificatePath,
            PrivateKeyPath = certificateFiles.PrivateKeyPath,
            MaxMessageSizeBytes = 1024,
            MaxConcurrentConnections = 1,
            MaxConcurrentConnectionsPerIp = 1,
        });

        await service.StartAsync(CancellationToken.None);
        try {
            await WaitForPortAsync(port, CancellationToken.None);
            using var firstClient = new TcpClient();
            await firstClient.ConnectAsync(System.Net.IPAddress.Loopback, port, CancellationToken.None);
            using var firstReader = new StreamReader(firstClient.GetStream());
            string? firstBanner = await firstReader.ReadLineAsync(CancellationToken.None);
            Assert.StartsWith("220", firstBanner, StringComparison.Ordinal);

            using var secondClient = new TcpClient();
            await secondClient.ConnectAsync(System.Net.IPAddress.Loopback, port, CancellationToken.None);
            using var secondReader = new StreamReader(secondClient.GetStream());
            Task<string?> secondBannerTask = secondReader.ReadLineAsync(CancellationToken.None).AsTask();

            await Assert.ThrowsAsync<TimeoutException>(
                () => secondBannerTask.WaitAsync(TimeSpan.FromMilliseconds(250), TimeProvider.System));

            firstClient.Dispose();
            string? secondBanner = await secondBannerTask.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            Assert.StartsWith("220", secondBanner, StringComparison.Ordinal);
        } finally {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SmtpHostedService_WhenPerIpConnectionLimitIsReached_ClosesExcessSession() {
        int port = GetFreeTcpPort();
        using CertificateFiles certificateFiles = CreateCertificateFiles("localhost");
        MailInboxSmtpHostedService service = CreateSmtpHostedService(new MailInboxSmtpOptions {
            Enabled = true,
            ServerName = "localhost",
            ListenAddress = System.Net.IPAddress.Loopback.ToString(),
            Port = port,
            CertificatePath = certificateFiles.CertificatePath,
            PrivateKeyPath = certificateFiles.PrivateKeyPath,
            MaxMessageSizeBytes = 1024,
            MaxConcurrentConnections = 2,
            MaxConcurrentConnectionsPerIp = 1,
        });

        await service.StartAsync(CancellationToken.None);
        try {
            await WaitForPortAsync(port, CancellationToken.None);
            using var firstClient = new TcpClient();
            await firstClient.ConnectAsync(System.Net.IPAddress.Loopback, port, CancellationToken.None);
            using var firstReader = new StreamReader(firstClient.GetStream());
            Assert.StartsWith(
                "220",
                await firstReader.ReadLineAsync(CancellationToken.None),
                StringComparison.Ordinal);

            using var excessClient = new TcpClient();
            await excessClient.ConnectAsync(System.Net.IPAddress.Loopback, port, CancellationToken.None);
            using var excessReader = new StreamReader(excessClient.GetStream());
            string? excessBanner = await excessReader.ReadLineAsync(CancellationToken.None)
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

            Assert.Null(excessBanner);
        } finally {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task RetentionHostedService_WhenStarted_UsesConfiguredCutoffsAndBatchSize() {
        var store = new RecordingRetentionStore(expectedCallCount: 1, failuresBeforeSuccess: 0);
        var options = new MailInboxStorageOptions {
            ContentRetention = TimeSpan.FromDays(14),
            MetadataRetention = TimeSpan.FromDays(90),
            CleanupInterval = TimeSpan.FromHours(1),
            CleanupBatchSize = 25,
        };
        var service = new MailInboxRetentionHostedService(
            store,
            Microsoft.Extensions.Options.Options.Create(options),
            FixedTime,
            NullLogger<MailInboxRetentionHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try {
            await store.ExpectedCallReached.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        } finally {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.Equal(FixedNow - options.ContentRetention, store.ContentCutoffUtc);
        Assert.Equal(FixedNow - options.MetadataRetention, store.MetadataCutoffUtc);
        Assert.Equal(options.CleanupBatchSize, store.BatchSize);
    }

    [Fact]
    public async Task RetentionHostedService_WhenPurgeFails_RetriesAfterConfiguredInterval() {
        var store = new RecordingRetentionStore(expectedCallCount: 2, failuresBeforeSuccess: 1);
        var service = new MailInboxRetentionHostedService(
            store,
            Microsoft.Extensions.Options.Options.Create(new MailInboxStorageOptions {
                CleanupInterval = TimeSpan.FromMilliseconds(10),
            }),
            FixedTime,
            NullLogger<MailInboxRetentionHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try {
            await store.ExpectedCallReached.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        } finally {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(store.CallCount >= 2);
    }

    [Fact]
    public async Task RetentionHostedService_WhenCanceledDuringPurge_StopsCleanly() {
        using var cts = new CancellationTokenSource();
        var service = new MailInboxRetentionHostedService(
            new CancelingRetentionStore(cts),
            Microsoft.Extensions.Options.Options.Create(new MailInboxStorageOptions()),
            FixedTime,
            NullLogger<MailInboxRetentionHostedService>.Instance);

        await InvokeExecuteAsync(service, cts.Token);
    }

    [Fact]
    public async Task RetentionHostedService_DrainExpiredAsync_ContinuesUntilBothBatchesArePartial() {
        var store = new DrainingRetentionStore([
            new InboundMailRetentionResult(ContentPurgedCount: 2, MetadataDeletedCount: 2),
            new InboundMailRetentionResult(ContentPurgedCount: 2, MetadataDeletedCount: 0),
            new InboundMailRetentionResult(ContentPurgedCount: 1, MetadataDeletedCount: 0),
        ]);
        var service = new MailInboxRetentionHostedService(
            store,
            Microsoft.Extensions.Options.Options.Create(new MailInboxStorageOptions { CleanupBatchSize = 2 }),
            FixedTime,
            NullLogger<MailInboxRetentionHostedService>.Instance);

        InboundMailRetentionResult result = await InvokeDrainExpiredAsync(service, CancellationToken.None);

        Assert.Equal(5, result.ContentPurgedCount);
        Assert.Equal(2, result.MetadataDeletedCount);
        Assert.Equal(3, store.CallCount);
    }

    private static int GetFreeTcpPort() {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, port: 0);
        listener.Start();
        try {
            return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        } finally {
            listener.Stop();
        }
    }

    private static CertificateFiles CreateCertificateFiles(
        string dnsName,
        DateTimeOffset? notBefore = null,
        DateTimeOffset? notAfter = null) {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={dnsName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName(dnsName);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(
            certificateAuthority: false,
            hasPathLengthConstraint: false,
            pathLengthConstraint: 0,
            critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        OidCollection enhancedKeyUsages = [new Oid(
            value: "1.3.6.1.5.5.7.3.1",
            friendlyName: "TLS Web Server Authentication")];
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, critical: true));

        using X509Certificate2 certificate = request.CreateSelfSigned(
            notBefore ?? DateTimeOffset.UtcNow.AddMinutes(-5),
            notAfter ?? DateTimeOffset.UtcNow.AddDays(1));
        string certificatePath = Path.GetTempFileName();
        string privateKeyPath = Path.GetTempFileName();
        File.WriteAllText(certificatePath, certificate.ExportCertificatePem());
        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        return new CertificateFiles(certificatePath, privateKeyPath, certificate.Thumbprint);
    }

    private static async Task<IReadOnlyList<string>> ReadSmtpResponseAsync(
        StreamReader reader,
        string expectedCode) {
        var lines = new List<string>();
        while (true) {
            string? line = await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(line);
            Assert.StartsWith(expectedCode, line, StringComparison.Ordinal);
            lines.Add(line);
            if (line.Length < 4 || line[3] != '-') {
                return lines;
            }
        }
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 8, 17, 6, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static MailInboxSmtpHostedService CreateSmtpHostedService(MailInboxSmtpOptions options) {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> optionsWrapper =
            Microsoft.Extensions.Options.Options.Create(options);
        var rateLimiter = new MailInboxSlidingWindowRateLimiter(optionsWrapper, TimeProvider.System);
        var messageStore = new SmtpInboundMessageStore(
            new ThrowingInboundMailStore(),
            optionsWrapper,
            rateLimiter,
            TimeProvider.System,
            NullLogger<SmtpInboundMessageStore>.Instance);
        var mailboxFilter = new MailInboxMailboxFilter(
            optionsWrapper,
            rateLimiter);
        return new MailInboxSmtpHostedService(
            optionsWrapper,
            messageStore,
            mailboxFilter,
            new MailInboxEndpointListenerFactory(optionsWrapper),
            NullLogger<MailInboxSmtpHostedService>.Instance);
    }

    private static async Task WaitForPortAsync(int port, CancellationToken cancellationToken) {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5)) {
            try {
                using var client = new TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, port, cancellationToken).ConfigureAwait(false);
                return;
            } catch (SocketException) {
                await Task.Delay(TimeSpan.FromMilliseconds(25), TimeProvider.System, cancellationToken).ConfigureAwait(false);
            }
        }

        Assert.Fail($"SMTP listener did not open port {port.ToString(CultureInfo.InvariantCulture)} before the timeout.");
    }

    private static async Task InvokeExecuteAsync(BackgroundService service, CancellationToken cancellationToken) {
        System.Reflection.MethodInfo method = service.GetType().GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [cancellationToken])!).ConfigureAwait(false);
    }

    private static async Task<InboundMailRetentionResult> InvokeDrainExpiredAsync(
        MailInboxRetentionHostedService service,
        CancellationToken cancellationToken) {
        System.Reflection.MethodInfo method = service.GetType().GetMethod(
            "DrainExpiredAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return await ((Task<InboundMailRetentionResult>)method.Invoke(service, [cancellationToken])!)
            .ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed record CertificateFiles(
        string CertificatePath,
        string PrivateKeyPath,
        string Thumbprint) : IDisposable {
        public void Dispose() {
            File.Delete(CertificatePath);
            File.Delete(PrivateKeyPath);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRetentionStore(
        int expectedCallCount,
        int failuresBeforeSuccess) : IInboundMailStore {
        private readonly TaskCompletionSource _expectedCallReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public Task ExpectedCallReached => _expectedCallReached.Task;

        public int CallCount => Volatile.Read(ref _callCount);

        public DateTimeOffset ContentCutoffUtc { get; private set; }

        public DateTimeOffset MetadataCutoffUtc { get; private set; }

        public int BatchSize { get; private set; }

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ContentCutoffUtc = contentCutoffUtc;
            MetadataCutoffUtc = metadataCutoffUtc;
            BatchSize = batchSize;
            int callCount = Interlocked.Increment(ref _callCount);
            if (callCount >= expectedCallCount) {
                _expectedCallReached.TrySetResult();
            }

            return callCount <= failuresBeforeSuccess
                ? Task.FromException<InboundMailRetentionResult>(new InvalidOperationException("transient"))
                : Task.FromResult(new InboundMailRetentionResult(ContentPurgedCount: 1, MetadataDeletedCount: 1));
        }

        public Task<InboundMailSaveResult> SaveAsync(
            InboundMailMessage message,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(
            Guid id,
            DateTimeOffset readAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingInboundMailStore : IInboundMailStore {
        public Task<InboundMailSaveResult> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancelingRetentionStore(CancellationTokenSource cancellationSource) : IInboundMailStore {
        public async Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) {
            await cancellationSource.CancelAsync().ConfigureAwait(false);
            return await Task.FromCanceled<InboundMailRetentionResult>(cancellationToken).ConfigureAwait(false);
        }

        public Task<InboundMailSaveResult> SaveAsync(
            InboundMailMessage message,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(
            Guid id,
            DateTimeOffset readAtUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class DrainingRetentionStore(IReadOnlyList<InboundMailRetentionResult> results) : IInboundMailStore {
        public int CallCount { get; private set; }

        public Task<InboundMailRetentionResult> PurgeExpiredAsync(
            DateTimeOffset contentCutoffUtc,
            DateTimeOffset metadataCutoffUtc,
            int batchSize,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            InboundMailRetentionResult result = results[CallCount++];
            return Task.FromResult(result);
        }

        public Task<InboundMailSaveResult> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(
            int limit,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
