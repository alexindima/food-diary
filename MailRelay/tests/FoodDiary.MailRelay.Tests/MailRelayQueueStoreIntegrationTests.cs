using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using FoodDiary.MailRelay.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task EmptyQueueQueries_ReturnEmptyResults() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        Assert.Empty(await store.ClaimDueBatchAsync(CancellationToken.None));
        Assert.Empty(await store.ClaimOutboxBatchAsync(CancellationToken.None));
        Assert.Null(await store.TryClaimMessageByIdAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Null(await store.GetMessageDetailsAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Empty(await store.GetDeliveryEventsAsync(email: null, CancellationToken.None));
        Assert.Empty(await store.GetSuppressionsAsync(email: null, CancellationToken.None));
        Assert.False(await store.RemoveSuppressionAsync("missing@example.com", CancellationToken.None));
    }

    [Fact]
    public void QueueRowMapper_ConvertsTimestampsAndNormalizesEmail() {
        var dateTimeOffset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(1));
        var dateTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);

        Assert.Equal(dateTimeOffset.ToUniversalTime(), MailRelayQueueRowMapper.ToDateTimeOffset(dateTimeOffset));
        Assert.Equal(DateTimeKind.Utc, MailRelayQueueRowMapper.ToDateTimeOffset(dateTime).UtcDateTime.Kind);
        Assert.Equal("user@example.com", MailRelayQueueRowMapper.NormalizeEmail(" USER@Example.COM "));
        Assert.Null(MailRelayQueueRowMapper.NormalizeEmail(" "));
        Assert.Throws<InvalidOperationException>(() => MailRelayQueueRowMapper.ToDateTimeOffset("unexpected"));
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
    public async Task EnqueueAsync_WhenRequestIsInvalid_ThrowsBeforeOpeningConnection() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        await Assert.ThrowsAsync<ArgumentException>(() => store.EnqueueAsync(
            CreateRequest() with { FromAddress = " " },
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => store.EnqueueAsync(
            CreateRequest() with { Subject = " " },
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => store.EnqueueAsync(
            CreateRequest() with { HtmlBody = " " },
            CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.EnqueueAsync(
            CreateRequest() with { To = [] },
            CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task Outbox_WhenPublishFails_IsMarkedForRetry() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);
        await store.EnqueueAsync(CreateRequest(), CancellationToken.None);
        MailRelayOutboxMessage outboxMessage = (await store.ClaimOutboxBatchAsync(CancellationToken.None)).Single();

        await store.MarkOutboxFailedAsync(outboxMessage.Id, outboxMessage.AttemptCount, "publish failed", CancellationToken.None);

        Assert.Equal("retry", await GetScalarAsync<string>(dataSource, "select status from mailrelay_outbox_messages where id = @id", outboxMessage.Id));
        Assert.Equal("publish failed", await GetScalarAsync<string>(dataSource, "select last_error from mailrelay_outbox_messages where id = @id", outboxMessage.Id));
    }

    [RequiresDockerFact]
    public async Task InboxClaim_WhenMessageIsProcessed_ReturnsDuplicateOnSecondClaim() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        MailRelayInboxClaimResult first = await store.TryClaimInboxMessageAsync("consumer", "message-key", CancellationToken.None);
        await store.MarkInboxProcessedAsync(first.InboxId, CancellationToken.None);
        MailRelayInboxClaimResult second = await store.TryClaimInboxMessageAsync("consumer", "message-key", CancellationToken.None);

        Assert.True(first.Claimed);
        Assert.False(second.Claimed);
        Assert.Equal(first.InboxId, second.InboxId);
    }

    [RequiresDockerFact]
    public async Task InboxClaim_WhenMessageFails_CanBeClaimedAgainWithLastErrorStored() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        MailRelayInboxClaimResult first = await store.TryClaimInboxMessageAsync("consumer", "message-key", CancellationToken.None);
        await store.MarkInboxFailedAsync(first.InboxId, "failed once", CancellationToken.None);
        MailRelayInboxClaimResult second = await store.TryClaimInboxMessageAsync("consumer", "message-key", CancellationToken.None);

        Assert.True(first.Claimed);
        Assert.True(second.Claimed);
        Assert.Equal(first.InboxId, second.InboxId);
        Assert.Equal("failed once", await GetScalarAsync<string>(dataSource, "select last_error from mailrelay_inbox_messages where id = @id", first.InboxId));
    }

    [RequiresDockerFact]
    public async Task GetStatsAsync_ReturnsCountsByQueueStatus() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);
        Guid sentId = await store.EnqueueAsync(CreateRequest(), CancellationToken.None);
        Guid suppressedId = await store.EnqueueAsync(CreateRequest(), CancellationToken.None);
        Guid retryId = await store.EnqueueAsync(CreateRequest(), CancellationToken.None);

        await store.MarkSentAsync(sentId, CancellationToken.None);
        await store.MarkSuppressedAsync(suppressedId, ["blocked@example.com"], CancellationToken.None);
        QueuedEmailMessage retryMessage = (await store.ClaimDueBatchAsync(CancellationToken.None))
            .Single(message => message.Id == retryId);
        await store.MarkFailedAttemptAsync(
            new QueuedEmailFailureDecision((QueuedEmailId)retryId, retryMessage.AttemptCount, QueuedEmailStatus.Retry, IsTerminalFailure: false, "retry"),
            CancellationToken.None);

        MailRelayQueueStats stats = await store.GetStatsAsync(CancellationToken.None);

        Assert.Equal(1, stats.RetryCount);
        Assert.Equal(1, stats.SentCount);
        Assert.Equal(1, stats.SuppressedCount);
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

    [RequiresDockerFact]
    public async Task GetSuppressedRecipientsAsync_ReturnsOnlyActiveNormalizedRecipients() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        MailRelayQueueStore store = CreateStore(dataSource);

        await store.UpsertSuppressionAsync(
            new CreateSuppressionRequest("BLOCKED@Example.COM", "manual", "test"),
            CancellationToken.None);
        await store.UpsertSuppressionAsync(
            new CreateSuppressionRequest("expired@example.com", "manual", "test", DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        IReadOnlyList<string> suppressed = await store.GetSuppressedRecipientsAsync(
            ["blocked@example.com", "expired@example.com", "allowed@example.com", ""],
            CancellationToken.None);

        Assert.Equal(["blocked@example.com"], suppressed);
        Assert.Empty(await store.GetSuppressedRecipientsAsync([], CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task MailRelayReadinessChecker_WhenPostgresBackendIsReady_Completes() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        var brokerOptions = new MailRelayBrokerOptions {
            Backend = MailRelayBrokerOptions.PostgresPollingBackend,
        };
        var checker = new MailRelayReadinessChecker(
            dataSource,
            new RabbitMqMailRelayBroker(
                Options.Create(brokerOptions),
                NullLogger<RabbitMqMailRelayBroker>.Instance),
            Options.Create(brokerOptions));

        await checker.CheckReadyAsync(CancellationToken.None);
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

    private static async Task<T> GetScalarAsync<T>(NpgsqlDataSource dataSource, string sql, Guid id) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                return (T)(await command.ExecuteScalarAsync().ConfigureAwait(false)
                           ?? throw new InvalidOperationException("Query did not return a value."));
            }
        }
    }
}
