using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;
using Npgsql;

namespace FoodDiary.MailInbox.IntegrationTests;

[Collection("mailinbox-postgres")]
[ExcludeFromCodeCoverage]
public sealed class NpgsqlInboundMailStoreIntegrationTests(MailInboxPostgresFixture fixture) {
    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_CreatesCurrentSchemaAndRecordsMigrations() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();

        Assert.Equal(7, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_schema_migrations"));
        Assert.Equal(
            "read_at_utc",
            await GetScalarAsync<string>(
                dataSource,
                """
                select column_name
                from information_schema.columns
                where table_schema = 'public'
                  and table_name = 'mailinbox_messages'
                  and column_name = 'read_at_utc'
                """));
        Assert.Equal(
            "bytea",
            await GetScalarAsync<string>(
                dataSource,
                """
                select data_type
                from information_schema.columns
                where table_schema = 'public'
                  and table_name = 'mailinbox_messages'
                  and column_name = 'raw_mime'
                """));
        Assert.Equal(
            5,
            await GetScalarAsync<long>(
                dataSource,
                """
                select count(*)
                from pg_constraint
                where conrelid = 'public.mailinbox_messages'::regclass
                  and conname in (
                      'ck_mailinbox_messages_message_id_length',
                      'ck_mailinbox_messages_from_address_length',
                      'ck_mailinbox_messages_envelope_from_address_length',
                      'ck_mailinbox_messages_subject_length',
                      'ck_mailinbox_messages_recipients_limits')
                  and convalidated
                """));
    }

    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_WhenCalledTwice_IsIdempotent() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        await store.EnsureSchemaAsync(CancellationToken.None);

        Assert.Equal(7, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_schema_migrations"));
    }

    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_WhenCalledConcurrently_SerializesMigrations() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            NpgsqlInboundMailStore[] stores = [.. Enumerable.Range(0, 4).Select(_ => CreateStore(dataSource))];

            await Task.WhenAll(stores.Select(store => store.EnsureSchemaAsync(CancellationToken.None)))
                .ConfigureAwait(false);

            Assert.Equal(
                7,
                await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_schema_migrations")
                    .ConfigureAwait(false));
        }
    }

    [RequiresDockerFact]
    public async Task SaveQueryAndMarkAsReadAsync_RoundTripsMessageState() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        var receivedAt = new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero);
        var message = InboundMailMessage.Receive(
            "message-id",
            "sender@example.com",
            ["dmarc@fooddiary.club"],
            "DMARC aggregate report",
            "plain",
            "<p>html</p>",
            "raw mime",
            receivedAt);

        Guid id = (await store.SaveAsync(
            message,
            new InboundMailAdmission(IsTrustedRelay: true, EnvelopeFromAddress: "bounce@relay.example"),
            CancellationToken.None)).Id;
        IReadOnlyList<InboundMailMessageSummary> summaries = await store.GetMessagesAsync(10, CancellationToken.None);
        InboundMailMessageDetails? details = await store.GetMessageDetailsAsync(id, CancellationToken.None);

        Assert.Single(summaries);
        Assert.Equal(id, summaries[0].Id);
        Assert.Equal(InboundMailMessageCategories.DmarcReport, summaries[0].Category);
        Assert.Null(summaries[0].ReadAtUtc);
        Assert.Equal("bounce@relay.example", summaries[0].EnvelopeFromAddress);
        Assert.True(summaries[0].IsTrustedRelay);
        Assert.False(summaries[0].FromAddressIsVerified);
        Assert.NotNull(details);
        Assert.Equal("raw mime", details.RawMime);
        Assert.Null(details.ReadAtUtc);
        Assert.Equal("bounce@relay.example", details.EnvelopeFromAddress);
        Assert.True(details.IsTrustedRelay);
        Assert.False(details.FromAddressIsVerified);
        Assert.False(details.DmarcReportIsVerified);

        var readAt = new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.FromHours(4));
        Assert.True(await store.MarkAsReadAsync(id, readAt, CancellationToken.None));
        Assert.False(await store.MarkAsReadAsync(Guid.NewGuid(), readAt, CancellationToken.None));
        string rowVersionAfterFirstRead = await GetScalarAsync<string>(
            dataSource,
            "select xmin::text from mailinbox_messages where id = @id",
            parameters => parameters.AddWithValue("id", id));
        Assert.True(await store.MarkAsReadAsync(id, readAt.AddHours(1), CancellationToken.None));
        string rowVersionAfterRepeatedRead = await GetScalarAsync<string>(
            dataSource,
            "select xmin::text from mailinbox_messages where id = @id",
            parameters => parameters.AddWithValue("id", id));

        InboundMailMessageDetails? readDetails = await store.GetMessageDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(readDetails);
        Assert.Equal(readAt.ToUniversalTime(), readDetails.ReadAtUtc);
        Assert.Equal(rowVersionAfterFirstRead, rowVersionAfterRepeatedRead);
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenOptionalFieldsAreMissing_PersistsNullsAndReceivedStatus() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        var message = InboundMailMessage.Receive(
            messageId: null,
            fromAddress: null,
            ["admin@fooddiary.club"],
            subject: null,
            textBody: null,
            htmlBody: null,
            "raw mime",
            new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero));

        Guid id = (await store.SaveAsync(message, CancellationToken.None)).Id;

        long matchingRows = await GetScalarAsync<long>(
            dataSource,
            """
            select count(*)
            from mailinbox_messages
            where id = @id
              and message_id is null
              and from_address is null
              and subject is null
              and text_body is null
              and html_body is null
              and raw_mime = convert_to('raw mime', 'UTF8')
              and status = 'received'
              and read_at_utc is null
            """,
            parameters => parameters.AddWithValue("id", id));
        Assert.Equal(1, matchingRows);
    }

    [RequiresDockerFact]
    public async Task GetMessagesAsync_WhenNoMessagesExist_ReturnsEmptyList() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        IReadOnlyList<InboundMailMessageSummary> summaries = await store.GetMessagesAsync(10, CancellationToken.None);

        Assert.Empty(summaries);
    }

    [RequiresDockerFact]
    public async Task GetMessagesAsync_ReturnsNewestMessagesUpToLimit() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        await store.SaveAsync(CreateMessage("older", new DateTimeOffset(2026, 6, 14, 9, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await store.SaveAsync(CreateMessage("newer", new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero)), CancellationToken.None);

        IReadOnlyList<InboundMailMessageSummary> summaries = await store.GetMessagesAsync(1, CancellationToken.None);

        Assert.Single(summaries);
        Assert.Equal("newer", summaries[0].Subject);
    }

    [RequiresDockerFact]
    public async Task GetMessageDetailsAsync_WhenMessageDoesNotExist_ReturnsNull() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        InboundMailMessageDetails? details = await store.GetMessageDetailsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(details);
    }

    [RequiresDockerFact]
    public async Task GetMessageDetailsAsync_WhenRequestsRunInParallel_BoundsParserConcurrency() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore seedingStore = CreateStore(dataSource);
        InboundMailSaveResult saved = await seedingStore.SaveAsync(
            CreateMessage("parallel-details", new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        var parser = new BlockingDmarcReportParser(expectedConcurrentCalls: 2);
        using NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions { MaxConcurrentMessageDetailReads = 2 },
            parser);

        Task<InboundMailMessageDetails?>[] reads = [.. Enumerable.Range(0, 6)
            .Select(_ => store.GetMessageDetailsAsync(saved.Id, CancellationToken.None))];
        await parser.WaitUntilExpectedConcurrencyAsync()
            .WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        Assert.Equal(2, parser.ActiveCalls);
        Assert.Equal(2, parser.MaxConcurrentCalls);
        Assert.All(reads, static read => Assert.False(read.IsCompleted));

        parser.Release();
        InboundMailMessageDetails?[] details = await Task.WhenAll(reads);

        Assert.All(details, static detail => Assert.NotNull(detail));
        Assert.Equal(2, parser.MaxConcurrentCalls);
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenSmtpDeliveryIsRetried_ReturnsExistingMessageWithoutConsumingQuotaTwice() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource, new MailInboxStorageOptions { MaxMessagesPerDay = 1 });
        var receivedAt = new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);
        InboundMailMessage first = CreateMessage("same", receivedAt);
        InboundMailMessage retry = CreateMessage("same", receivedAt.AddMinutes(5));

        InboundMailSaveResult firstResult = await store.SaveAsync(first, CancellationToken.None);
        InboundMailSaveResult retryResult = await store.SaveAsync(retry, CancellationToken.None);

        Assert.False(firstResult.WasDuplicate);
        Assert.True(retryResult.WasDuplicate);
        Assert.Equal(firstResult.Id, retryResult.Id);
        Assert.Equal(1, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
        Assert.Equal(1, await GetScalarAsync<long>(dataSource, "select message_count from mailinbox_daily_ingestion_usage"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenRetryCrossesFixedBucketBoundary_DeduplicatesWithinSlidingWindow() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions { DeduplicationWindow = TimeSpan.FromHours(1) });
        var receivedAt = new DateTimeOffset(2026, 6, 18, 11, 59, 0, TimeSpan.Zero);

        InboundMailSaveResult first = await store.SaveAsync(CreateMessage("boundary", receivedAt), CancellationToken.None);
        InboundMailSaveResult retry = await store.SaveAsync(
            CreateMessage("boundary", receivedAt.AddMinutes(2)),
            CancellationToken.None);

        Assert.False(first.WasDuplicate);
        Assert.True(retry.WasDuplicate);
        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(1, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenSameContentArrivesOutsideSlidingWindow_PersistsNewMessage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions { DeduplicationWindow = TimeSpan.FromHours(1) });
        var receivedAt = new DateTimeOffset(2026, 6, 18, 11, 0, 0, TimeSpan.Zero);

        InboundMailSaveResult first = await store.SaveAsync(CreateMessage("repeat", receivedAt), CancellationToken.None);
        InboundMailSaveResult later = await store.SaveAsync(
            CreateMessage("repeat", receivedAt.AddHours(2)),
            CancellationToken.None);

        Assert.False(first.WasDuplicate);
        Assert.False(later.WasDuplicate);
        Assert.NotEqual(first.Id, later.Id);
        Assert.Equal(2, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenIdenticalMessagesArriveConcurrently_PersistsOneMessage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        var receivedAt = new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);

        InboundMailSaveResult[] results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => store.SaveAsync(CreateMessage("concurrent", receivedAt), CancellationToken.None)));

        Assert.Single(results, static result => !result.WasDuplicate);
        Assert.Equal(3, results.Count(static result => result.WasDuplicate));
        Assert.Single(results.Select(static result => result.Id).Distinct());
        Assert.Equal(1, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenDailyQuotaIsExhausted_RollsBackNewMessage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource, new MailInboxStorageOptions { MaxMessagesPerDay = 1 });
        var receivedAt = new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);
        await store.SaveAsync(CreateMessage("first", receivedAt), CancellationToken.None);

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(
            () => store.SaveAsync(CreateMessage("second", receivedAt), CancellationToken.None));

        Assert.Equal("InboundMailStorageQuotaExceededException", exception.GetType().Name);
        Assert.Equal(1, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenUntrustedQuotaIsExhausted_PreservesCapacityForTrustedRelay() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions {
                MaxMessagesPerDay = 2,
                MaxUntrustedMessagesPerDay = 1,
            });
        var receivedAt = new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero);
        await store.SaveAsync(CreateMessage("untrusted-first", receivedAt), CancellationToken.None);

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            store.SaveAsync(CreateMessage("untrusted-second", receivedAt), CancellationToken.None));
        Assert.Equal("InboundMailStorageQuotaExceededException", exception.GetType().Name);
        InboundMailSaveResult trusted = await store.SaveAsync(
            CreateMessage("trusted", receivedAt),
            InboundMailAdmission.TrustedRelay,
            CancellationToken.None);
        long messageCount = await GetScalarAsync<long>(
            dataSource,
            "select message_count from mailinbox_daily_ingestion_usage");
        long untrustedMessageCount = await GetScalarAsync<long>(
            dataSource,
            "select untrusted_message_count from mailinbox_daily_ingestion_usage");

        Assert.Multiple(
            () => Assert.False(trusted.WasDuplicate),
            () => Assert.Equal(2, messageCount),
            () => Assert.Equal(1, untrustedMessageCount));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenFirstMessageExceedsDailyByteQuota_RollsBackMessageAndUsage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions { MaxRawBytesPerDay = 1 });

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(
            () => store.SaveAsync(
                CreateMessage("larger-than-daily-quota", new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero)),
                CancellationToken.None));

        Assert.Equal("InboundMailStorageQuotaExceededException", exception.GetType().Name);
        Assert.Equal(0, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
        Assert.Equal(0, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_daily_ingestion_usage"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenPersistedCopiesExceedDailyByteQuota_RollsBackMessageAndUsage() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        InboundMailMessage message = CreateMessage(
            new string('s', 100),
            new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero));
        NpgsqlInboundMailStore store = CreateStore(
            dataSource,
            new MailInboxStorageOptions { MaxRawBytesPerDay = message.RawMimeBytes.Length });

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(
            () => store.SaveAsync(message, CancellationToken.None));

        Assert.Equal("InboundMailStorageQuotaExceededException", exception.GetType().Name);
        Assert.Equal(0, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
        Assert.Equal(0, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_daily_ingestion_usage"));
    }

    [RequiresDockerFact]
    public async Task SaveAsync_WhenSubjectExceedsStoredMetadataLimit_RejectsBeforeDatabaseAccess() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        InboundMailMessage message = CreateMessage(
            new string('s', 999),
            new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero));

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(message, CancellationToken.None));

        Assert.Equal(0, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_messages"));
    }

    [RequiresDockerFact]
    public async Task Schema_WhenRecipientExceedsStoredMetadataLimit_RejectsDirectUpdate() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        InboundMailSaveResult saved = await store.SaveAsync(
            CreateMessage("bounded-recipient", new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        string recipientsJson = System.Text.Json.JsonSerializer.Serialize(new[] { new string('r', 321) });

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() => GetScalarAsync<int>(
            dataSource,
            "update mailinbox_messages set to_recipients_json = @recipients_json::jsonb where id = @id; select 1;",
            parameters => {
                parameters.AddWithValue("recipients_json", recipientsJson);
                parameters.AddWithValue("id", saved.Id);
            }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [RequiresDockerFact]
    public async Task PurgeExpiredAsync_RemovesContentBeforeDeletingOlderMetadata() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);
        InboundMailSaveResult metadataExpired = await store.SaveAsync(
            CreateMessage("metadata-expired", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        InboundMailSaveResult contentExpired = await store.SaveAsync(
            CreateMessage("content-expired", new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);
        InboundMailSaveResult current = await store.SaveAsync(
            CreateMessage("current", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        InboundMailRetentionResult result = await store.PurgeExpiredAsync(
            new DateTimeOffset(2026, 5, 19, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 6, 18, 0, 0, 0, TimeSpan.Zero),
            batchSize: 10,
            CancellationToken.None);

        Assert.Equal(1, result.ContentPurgedCount);
        Assert.Equal(1, result.MetadataDeletedCount);
        Assert.Null(await store.GetMessageDetailsAsync(metadataExpired.Id, CancellationToken.None));
        InboundMailMessageDetails? purgedDetails = await store.GetMessageDetailsAsync(contentExpired.Id, CancellationToken.None);
        Assert.NotNull(purgedDetails);
        Assert.Null(purgedDetails.RawMime);
        Assert.Null(purgedDetails.TextBody);
        Assert.Equal(FixedTime.GetUtcNow(), purgedDetails.ContentPurgedAtUtc);
        InboundMailMessageDetails? currentDetails = await store.GetMessageDetailsAsync(current.Id, CancellationToken.None);
        Assert.NotNull(currentDetails);
        Assert.NotNull(currentDetails.RawMime);
    }

    [RequiresDockerFact]
    public async Task PurgeExpiredAsync_WhenMetadataCutoffIsNewerThanContentCutoff_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        await Assert.ThrowsAsync<ArgumentException>(() => store.PurgeExpiredAsync(
            contentCutoffUtc: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            metadataCutoffUtc: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
            batchSize: 10,
            CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenSchemaIsReady_Completes() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        await checker.CheckReadyAsync(CancellationToken.None);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenSchemaIsMissing_Throws() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => checker.CheckReadyAsync(CancellationToken.None)).ConfigureAwait(false);
            Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
        }
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenRequiredIndexIsMissing_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "drop index ix_mailinbox_messages_ingestion_received_at_utc; select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenRequiredColumnIsMissing_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "alter table mailinbox_messages drop column status; select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenPrimaryKeyIsMissing_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "alter table mailinbox_messages drop constraint mailinbox_messages_pkey; select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenRequiredIndexTargetsWrongColumns_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "drop index ux_mailinbox_messages_ingestion_window; " +
            "create unique index ux_mailinbox_messages_ingestion_window on mailinbox_messages (id); " +
            "select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenMetadataConstraintIsMissing_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "alter table mailinbox_messages drop constraint ck_mailinbox_messages_subject_length; select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenMetadataConstraintHasWrongExpression_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "alter table mailinbox_messages drop constraint ck_mailinbox_messages_subject_length; " +
            "alter table mailinbox_messages add constraint ck_mailinbox_messages_subject_length check (true); " +
            "select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task ReadinessChecker_WhenLatestMigrationRecordIsMissing_Throws() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        await GetScalarAsync<int>(
            dataSource,
            "delete from mailinbox_schema_migrations where name = '202608200002_add_mail_authentication_provenance'; select 1;");
        var checker = new NpgsqlMailInboxReadinessChecker(dataSource);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => checker.CheckReadyAsync(CancellationToken.None));

        Assert.Contains("schema is not ready", exception.Message, StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task RuntimeRoleProvisioner_GrantsDmlAndRejectsDdl() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var migrationDataSource = NpgsqlDataSource.Create(connectionString);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable migrationDataSourceLifetime =
            migrationDataSource.ConfigureAwait(false);
        await CreateStore(migrationDataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        string roleName = $"mailinbox_runtime_{Guid.NewGuid():N}";
        const string password = "0123456789abcdef0123456789abcdef";
        const string rotatedPassword = "fedcba9876543210fedcba9876543210";
        var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(migrationDataSource);
        string quotedRole = new NpgsqlCommandBuilder().QuoteIdentifier(roleName);
        string futureTableName = $"mailinbox_future_{Guid.NewGuid():N}";
        string quotedFutureTable = new NpgsqlCommandBuilder().QuoteIdentifier(futureTableName);

        try {
            await provisioner.ProvisionAsync(roleName, password, CancellationToken.None).ConfigureAwait(false);
            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"grant create on schema public to {quotedRole}; select 1;")
                .ConfigureAwait(false);
            await provisioner.ProvisionAsync(roleName, rotatedPassword, CancellationToken.None).ConfigureAwait(false);
            var runtimeBuilder = new NpgsqlConnectionStringBuilder(connectionString) {
                Username = roleName,
                Password = rotatedPassword,
                Pooling = false,
            };
            var runtimeDataSource = NpgsqlDataSource.Create(runtimeBuilder.ConnectionString);
            await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable runtimeDataSourceLifetime =
                runtimeDataSource.ConfigureAwait(false);
            var runtimeRoleValidator = new NpgsqlMailInboxRuntimeRoleValidator(runtimeDataSource);

            await runtimeRoleValidator.ValidateAsync(roleName, CancellationToken.None).ConfigureAwait(false);

            NpgsqlInboundMailStore runtimeStore = CreateStore(runtimeDataSource);
            InboundMailSaveResult saved = await runtimeStore.SaveAsync(
                    CreateMessage("runtime-dml", new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero)),
                    CancellationToken.None)
                .ConfigureAwait(false);
            InboundMailMessageDetails? details = await runtimeStore
                .GetMessageDetailsAsync(saved.Id, CancellationToken.None)
                .ConfigureAwait(false);
            bool markedRead = await runtimeStore
                .MarkAsReadAsync(saved.Id, DateTimeOffset.UtcNow, CancellationToken.None)
                .ConfigureAwait(false);
            bool noInherit = await GetScalarAsync<bool>(
                    migrationDataSource,
                    "select not rolinherit from pg_roles where rolname = @role_name",
                    parameters => parameters.AddWithValue("role_name", roleName))
                .ConfigureAwait(false);

            Assert.Multiple(
                () => Assert.Equal("runtime-dml", details?.Subject),
                () => Assert.True(markedRead),
                () => Assert.True(noInherit));

            PostgresException migrationMutationException = await Assert.ThrowsAsync<PostgresException>(() =>
                    GetScalarAsync<int>(
                        runtimeDataSource,
                        "update mailinbox_schema_migrations set applied_at_utc = applied_at_utc; select 1;"))
                .ConfigureAwait(false);
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, migrationMutationException.SqlState);

            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"create table {quotedFutureTable} (id integer); select 1;")
                .ConfigureAwait(false);
            PostgresException futureTableException = await Assert.ThrowsAsync<PostgresException>(() =>
                    GetScalarAsync<int>(runtimeDataSource, $"select count(*)::integer from {quotedFutureTable};"))
                .ConfigureAwait(false);
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, futureTableException.SqlState);

            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"grant select on table {quotedFutureTable} to {quotedRole}; select 1;")
                .ConfigureAwait(false);
            InvalidOperationException overgrantedRoleException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    runtimeRoleValidator.ValidateAsync(roleName, CancellationToken.None))
                .ConfigureAwait(false);
            Assert.Contains("least-privilege grants", overgrantedRoleException.Message, StringComparison.Ordinal);
            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"revoke all privileges on table {quotedFutureTable} from {quotedRole}; select 1;")
                .ConfigureAwait(false);

            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"alter role {quotedRole} superuser; select 1;")
                .ConfigureAwait(false);
            InvalidOperationException privilegedRoleException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    runtimeRoleValidator.ValidateAsync(roleName, CancellationToken.None))
                .ConfigureAwait(false);
            Assert.Contains("least-privilege grants", privilegedRoleException.Message, StringComparison.Ordinal);
            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"alter role {quotedRole} nosuperuser; select 1;")
                .ConfigureAwait(false);

            PostgresException exception = await Assert.ThrowsAsync<PostgresException>(() =>
                GetScalarAsync<int>(runtimeDataSource, "create table forbidden_runtime_ddl (id integer); select 1;")).ConfigureAwait(false);
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
        } finally {
            await GetScalarAsync<int>(
                    migrationDataSource,
                    $"drop table if exists {quotedFutureTable}; " +
                    $"drop owned by {quotedRole}; drop role if exists {quotedRole}; select 1;")
                .ConfigureAwait(false);
        }
    }

    [RequiresDockerFact]
    public async Task RuntimeRoleProvisioner_WhenExistingRoleHasMembership_RejectsReuse() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            string groupRoleName = $"mailinbox_group_{Guid.NewGuid():N}";
            string runtimeRoleName = $"mailinbox_runtime_{Guid.NewGuid():N}";
            string quotedGroupRole = new NpgsqlCommandBuilder().QuoteIdentifier(groupRoleName);
            string quotedRuntimeRole = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRoleName);
            var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

            try {
                await GetScalarAsync<int>(
                        dataSource,
                        $"create role {quotedGroupRole}; create role {quotedRuntimeRole} login; " +
                        $"grant {quotedGroupRole} to {quotedRuntimeRole}; select 1;")
                    .ConfigureAwait(false);

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        provisioner.ProvisionAsync(
                            runtimeRoleName,
                            "0123456789abcdef0123456789abcdef",
                            CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.Contains("not provisioner-owned or has unsafe settings", exception.Message, StringComparison.Ordinal);
            } finally {
                await GetScalarAsync<int>(
                        dataSource,
                        $"revoke {quotedGroupRole} from {quotedRuntimeRole}; " +
                        $"drop role if exists {quotedRuntimeRole}; drop role if exists {quotedGroupRole}; select 1;")
                    .ConfigureAwait(false);
            }
        }
    }

    [RequiresDockerFact]
    public async Task RuntimeRoleProvisioner_WhenExistingRoleOwnsObject_RejectsReuse() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
            string runtimeRoleName = $"mailinbox_runtime_{Guid.NewGuid():N}";
            string tableName = $"owned_{Guid.NewGuid():N}";
            string quotedRuntimeRole = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRoleName);
            string quotedTable = new NpgsqlCommandBuilder().QuoteIdentifier(tableName);
            var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

            try {
                await GetScalarAsync<int>(
                        dataSource,
                        $"create role {quotedRuntimeRole} login; create table {quotedTable} (id integer); " +
                        $"alter table {quotedTable} owner to {quotedRuntimeRole}; select 1;")
                    .ConfigureAwait(false);

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        provisioner.ProvisionAsync(
                            runtimeRoleName,
                            "0123456789abcdef0123456789abcdef",
                            CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.Contains("not provisioner-owned or has unsafe settings", exception.Message, StringComparison.Ordinal);
            } finally {
                await GetScalarAsync<int>(
                        dataSource,
                        $"drop table if exists {quotedTable}; drop role if exists {quotedRuntimeRole}; select 1;")
                    .ConfigureAwait(false);
            }
        }
    }

    [RequiresDockerFact]
    public async Task RuntimeRoleProvisioner_WhenProvisionedRoleHasRoleSettings_RejectsReuse() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
            string runtimeRoleName = $"mailinbox_runtime_{Guid.NewGuid():N}";
            string quotedRuntimeRole = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRoleName);
            var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

            try {
                await provisioner.ProvisionAsync(
                        runtimeRoleName,
                        "0123456789abcdef0123456789abcdef",
                        CancellationToken.None)
                    .ConfigureAwait(false);
                await GetScalarAsync<int>(
                        dataSource,
                        $"alter role {quotedRuntimeRole} set search_path = public, pg_catalog; select 1;")
                    .ConfigureAwait(false);

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        provisioner.ProvisionAsync(
                            runtimeRoleName,
                            "fedcba9876543210fedcba9876543210",
                            CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.Contains("not provisioner-owned or has unsafe settings", exception.Message, StringComparison.Ordinal);
            } finally {
                bool roleExists = await GetScalarAsync<bool>(
                        dataSource,
                        "select exists (select 1 from pg_roles where rolname = @role_name)",
                        parameters => parameters.AddWithValue("role_name", runtimeRoleName))
                    .ConfigureAwait(false);
                if (roleExists) {
                    await GetScalarAsync<int>(
                            dataSource,
                            $"alter role {quotedRuntimeRole} reset all; drop owned by {quotedRuntimeRole}; " +
                            $"drop role {quotedRuntimeRole}; select 1;")
                        .ConfigureAwait(false);
                }
            }
        }
    }

    [RequiresDockerFact]
    public async Task RuntimeRoleProvisioner_WhenProvisionedRoleHasCrossDatabaseGrant_RejectsReuse() {
        fixture.EnsureAvailable();
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (dataSource.ConfigureAwait(false)) {
            await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
            string runtimeRoleName = $"mailinbox_runtime_{Guid.NewGuid():N}";
            string externalDatabaseName = $"mailinbox_external_{Guid.NewGuid():N}";
            string quotedRuntimeRole = new NpgsqlCommandBuilder().QuoteIdentifier(runtimeRoleName);
            string quotedExternalDatabase = new NpgsqlCommandBuilder().QuoteIdentifier(externalDatabaseName);
            var provisioner = new NpgsqlMailInboxRuntimeRoleProvisioner(dataSource);

            try {
                await provisioner.ProvisionAsync(
                        runtimeRoleName,
                        "0123456789abcdef0123456789abcdef",
                        CancellationToken.None)
                    .ConfigureAwait(false);
                await ExecuteNonQueryAsync(dataSource, $"create database {quotedExternalDatabase}").ConfigureAwait(false);
                await ExecuteNonQueryAsync(
                        dataSource,
                        $"grant connect on database {quotedExternalDatabase} to {quotedRuntimeRole}")
                    .ConfigureAwait(false);

                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        provisioner.ProvisionAsync(
                            runtimeRoleName,
                            "fedcba9876543210fedcba9876543210",
                            CancellationToken.None))
                    .ConfigureAwait(false);

                Assert.Contains("cross-database grants", exception.Message, StringComparison.Ordinal);
            } finally {
                bool externalDatabaseExists = await GetScalarAsync<bool>(
                        dataSource,
                        "select exists (select 1 from pg_database where datname = @database_name)",
                        parameters => parameters.AddWithValue("database_name", externalDatabaseName))
                    .ConfigureAwait(false);
                if (externalDatabaseExists) {
                    await ExecuteNonQueryAsync(
                            dataSource,
                            $"revoke connect on database {quotedExternalDatabase} from {quotedRuntimeRole}")
                        .ConfigureAwait(false);
                    await ExecuteNonQueryAsync(dataSource, $"drop database {quotedExternalDatabase}").ConfigureAwait(false);
                }

                bool roleExists = await GetScalarAsync<bool>(
                        dataSource,
                        "select exists (select 1 from pg_roles where rolname = @role_name)",
                        parameters => parameters.AddWithValue("role_name", runtimeRoleName))
                    .ConfigureAwait(false);
                if (roleExists) {
                    await GetScalarAsync<int>(
                            dataSource,
                            $"drop owned by {quotedRuntimeRole}; drop role {quotedRuntimeRole}; select 1;")
                        .ConfigureAwait(false);
                }
            }
        }
    }

    private async Task<NpgsqlDataSource> CreateDataSourceAsync() {
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        return dataSource;
    }

    private static NpgsqlInboundMailStore CreateStore(
        NpgsqlDataSource dataSource,
        MailInboxStorageOptions? options = null,
        IMailInboxDmarcReportParser? parser = null) =>
        new(
            dataSource,
            parser ?? new DmarcReportParser(),
            Microsoft.Extensions.Options.Options.Create(options ?? new MailInboxStorageOptions()),
            FixedTime);

    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
    }

    [ExcludeFromCodeCoverage]
    private sealed class BlockingDmarcReportParser(int expectedConcurrentCalls) : IMailInboxDmarcReportParser {
        private readonly TaskCompletionSource _expectedConcurrencyReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _release = new(initialState: false);
        private int _activeCalls;
        private int _maxConcurrentCalls;

        public int ActiveCalls => Volatile.Read(ref _activeCalls);

        public int MaxConcurrentCalls => Volatile.Read(ref _maxConcurrentCalls);

        public Task WaitUntilExpectedConcurrencyAsync() => _expectedConcurrencyReached.Task;

        public void Release() => _release.Set();

        public DmarcReportPreview? TryParse(string rawMime, CancellationToken cancellationToken = default) {
            int activeCalls = Interlocked.Increment(ref _activeCalls);
            UpdateMaximum(activeCalls);
            if (activeCalls >= expectedConcurrentCalls) {
                _expectedConcurrencyReached.TrySetResult();
            }

            try {
                _release.Wait(cancellationToken);
                return null;
            } finally {
                Interlocked.Decrement(ref _activeCalls);
            }
        }

        private void UpdateMaximum(int value) {
            int current = Volatile.Read(ref _maxConcurrentCalls);
            while (value > current) {
                int observed = Interlocked.CompareExchange(ref _maxConcurrentCalls, value, current);
                if (observed == current) {
                    return;
                }

                current = observed;
            }
        }
    }

    private static InboundMailMessage CreateMessage(string subject, DateTimeOffset receivedAtUtc) =>
        InboundMailMessage.Receive(
            messageId: null,
            fromAddress: "sender@example.com",
            ["admin@fooddiary.club"],
            subject,
            textBody: null,
            htmlBody: null,
            $"raw mime {subject}",
            receivedAtUtc);

    private static Task<T> GetScalarAsync<T>(NpgsqlDataSource dataSource, string sql) =>
        GetScalarAsync<T>(dataSource, sql, configureParameters: null);

    private static async Task<T> GetScalarAsync<T>(
        NpgsqlDataSource dataSource,
        string sql,
        Action<NpgsqlParameterCollection>? configureParameters) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                configureParameters?.Invoke(command.Parameters);
                return (T)(await command.ExecuteScalarAsync().ConfigureAwait(false)
                           ?? throw new InvalidOperationException("Query did not return a value."));
            }
        }
    }

    private static async Task ExecuteNonQueryAsync(NpgsqlDataSource dataSource, string sql) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync().ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
