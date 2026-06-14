using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxHostedServiceTests {
    [Fact]
    public async Task SchemaInitializerHostedService_StartAsync_EnsuresSchema() {
        using var cts = new CancellationTokenSource();
        var initializer = new RecordingSchemaInitializer();
        var service = new MailInboxSchemaInitializerHostedService(
            initializer,
            NullLogger<MailInboxSchemaInitializerHostedService>.Instance);

        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        Assert.True(initializer.Called);
        Assert.Equal(cts.Token, initializer.CancellationToken);
    }

    [Fact]
    public async Task SmtpHostedService_WhenDisabled_CompletesWithoutStartingListener() {
        var messageStore = new SmtpInboundMessageStore(
            new ThrowingInboundMailStore(),
            NullLogger<SmtpInboundMessageStore>.Instance);
        var mailboxFilter = new MailInboxMailboxFilter(Options.Create(new MailInboxSmtpOptions()));
        var service = new MailInboxSmtpHostedService(
            Options.Create(new MailInboxSmtpOptions {
                Enabled = false,
            }),
            messageStore,
            mailboxFilter,
            NullLogger<MailInboxSmtpHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SmtpHostedService_WhenEnabled_StartsListenerUntilStopped() {
        int port = GetFreeTcpPort();
        var messageStore = new SmtpInboundMessageStore(
            new ThrowingInboundMailStore(),
            NullLogger<SmtpInboundMessageStore>.Instance);
        var mailboxFilter = new MailInboxMailboxFilter(Options.Create(new MailInboxSmtpOptions()));
        var service = new MailInboxSmtpHostedService(
            Options.Create(new MailInboxSmtpOptions {
                Enabled = true,
                ServerName = "localhost",
                Port = port,
                MaxMessageSizeBytes = 1024,
            }),
            messageStore,
            mailboxFilter,
            NullLogger<MailInboxSmtpHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try {
            Assert.True(await WaitForPortAsync(port, CancellationToken.None));
        } finally {
            await service.StopAsync(CancellationToken.None);
        }
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

    private static async Task<bool> WaitForPortAsync(int port, CancellationToken cancellationToken) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        while (!linked.IsCancellationRequested) {
            try {
                using var client = new TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, port, linked.Token).ConfigureAwait(false);
                return true;
            } catch (SocketException) {
                await Task.Delay(TimeSpan.FromMilliseconds(25), linked.Token).ConfigureAwait(false);
            }
        }

        return false;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSchemaInitializer : IMailInboxSchemaInitializer {
        public bool Called { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task EnsureSchemaAsync(CancellationToken cancellationToken) {
            Called = true;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingInboundMailStore : IInboundMailStore {
        public Task<Guid> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) =>
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
