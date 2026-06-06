using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    }
}
