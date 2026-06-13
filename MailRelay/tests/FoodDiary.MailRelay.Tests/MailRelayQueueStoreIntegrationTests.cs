using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using FoodDiary.MailRelay.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.MailRelay.Tests;

[Collection("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class MailRelayQueueStoreIntegrationTests(MailRelayEnvironmentFixture fixture) {
    [RequiresDockerFact]
    public async Task EnqueueClaimAndMarkSentAsync_UpdatesMessageLifecycle() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);
        Guid id = await store.EnqueueAsync(CreateRequest(), CancellationToken.None);

        IReadOnlyList<QueuedEmailMessage> claimed = await store.ClaimDueBatchAsync(CancellationToken.None);
        Assert.Single(claimed);
        Assert.Equal(id, claimed[0].Id);
        Assert.Equal(1, claimed[0].AttemptCount);

        await store.MarkSentAsync(id, CancellationToken.None);

        MailRelayMessageDetails? details = await store.GetMessageDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal(QueuedEmailStatus.Sent, details.Status);
        Assert.NotNull(details.SentAtUtc);
    }

    [RequiresDockerFact]
    public async Task MarkFailedAttemptAsync_WhenDecisionIsRetry_StoresRetryStateAndError() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);
        Guid id = await store.EnqueueAsync(CreateRequest(), CancellationToken.None);
        QueuedEmailMessage message = (await store.ClaimDueBatchAsync(CancellationToken.None)).Single();

        await store.MarkFailedAttemptAsync(
            new QueuedEmailFailureDecision((QueuedEmailId)id, message.AttemptCount, QueuedEmailStatus.Retry, IsTerminalFailure: false, "SMTP failed"),
            CancellationToken.None);

        MailRelayMessageDetails? details = await store.GetMessageDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal(QueuedEmailStatus.Retry, details.Status);
        Assert.Contains("SMTP failed", details.LastError, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task EnqueueAsync_WhenIdempotencyKeyAlreadyExists_DoesNotCreateAnotherOutboxMessage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);
        RelayEmailMessageRequest request = CreateRequest();

        Guid firstId = await store.EnqueueAsync(request, CancellationToken.None);
        Guid secondId = await store.EnqueueAsync(request, CancellationToken.None);

        Assert.Equal(firstId, secondId);
        Assert.Equal(1, await CountRowsAsync(dataSource, "mailrelay_outbox_messages"));
    }

    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_RecordsBaselineSchemaVersion() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();

        Assert.Equal(1, await CountRowsAsync(dataSource, "mailrelay_schema_versions"));
    }

    [RequiresDockerFact]
    public async Task SuppressionsAndDeliveryEvents_AreStoredAndQueried() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        await store.UpsertSuppressionAsync(
            new CreateSuppressionRequest("USER@Example.COM", "hard-bounce", "test"),
            CancellationToken.None);
        IReadOnlyList<MailRelaySuppressionEntry> suppressions = await store.GetSuppressionsAsync("user@example.com", CancellationToken.None);
        Assert.Single(suppressions);
        Assert.Equal("user@example.com", suppressions[0].Email);

        MailRelayDeliveryEventEntry deliveryEvent = await store.RecordDeliveryEventAsync(
            new IngestMailEventRequest("bounce", "user@example.com", "test", "hard", "provider-id", "hard-bounce"),
            CancellationToken.None);
        IReadOnlyList<MailRelayDeliveryEventEntry> deliveryEvents = await store.GetDeliveryEventsAsync("user@example.com", CancellationToken.None);

        Assert.Equal("bounce", deliveryEvent.EventType);
        Assert.Single(deliveryEvents);
        Assert.Equal("hard", deliveryEvents[0].Classification);

        Assert.True(await store.RemoveSuppressionAsync("user@example.com", CancellationToken.None));
        Assert.Empty(await store.GetSuppressionsAsync("user@example.com", CancellationToken.None));
    }

    private async Task<NpgsqlDataSource> CreateDataSourceAsync() {
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        return dataSource;
    }

    private static MailRelayQueueStore CreateStore(NpgsqlDataSource dataSource) =>
        new(dataSource, Options.Create(new MailRelayQueueOptions {
            BatchSize = 10,
            MaxAttempts = 2,
            BaseRetryDelaySeconds = 1,
            MaxRetryDelaySeconds = 1,
            LockTimeoutSeconds = 1,
            PollIntervalSeconds = 1,
        }));

    private static RelayEmailMessageRequest CreateRequest() =>
        new(
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            "correlation",
            Guid.NewGuid().ToString("N"));

    private static async Task<long> CountRowsAsync(NpgsqlDataSource dataSource, string tableName) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand($"select count(*) from {tableName}", connection);
            return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false) ?? 0L);
        }
    }
}
