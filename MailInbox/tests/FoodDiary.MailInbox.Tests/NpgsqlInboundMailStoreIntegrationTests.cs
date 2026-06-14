using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Tests.TestInfrastructure;
using Npgsql;

namespace FoodDiary.MailInbox.Tests;

[Collection("mailinbox-postgres")]
[ExcludeFromCodeCoverage]
public sealed class NpgsqlInboundMailStoreIntegrationTests(MailInboxPostgresFixture fixture) {
    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_CreatesCurrentSchemaAndRecordsMigrations() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();

        Assert.Equal(2, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_schema_migrations"));
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
    }

    [RequiresDockerFact]
    public async Task EnsureSchemaAsync_WhenCalledTwice_IsIdempotent() {
        fixture.EnsureAvailable();
        await using NpgsqlDataSource dataSource = await CreateDataSourceAsync();
        NpgsqlInboundMailStore store = CreateStore(dataSource);

        await store.EnsureSchemaAsync(CancellationToken.None);

        Assert.Equal(2, await GetScalarAsync<long>(dataSource, "select count(*) from mailinbox_schema_migrations"));
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

        Guid id = await store.SaveAsync(message, CancellationToken.None);
        IReadOnlyList<InboundMailMessageSummary> summaries = await store.GetMessagesAsync(10, CancellationToken.None);
        InboundMailMessageDetails? details = await store.GetMessageDetailsAsync(id, CancellationToken.None);

        Assert.Single(summaries);
        Assert.Equal(id, summaries[0].Id);
        Assert.Equal(InboundMailMessageCategories.DmarcReport, summaries[0].Category);
        Assert.Null(summaries[0].ReadAtUtc);
        Assert.NotNull(details);
        Assert.Equal("raw mime", details.RawMime);
        Assert.Null(details.ReadAtUtc);

        var readAt = new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.FromHours(4));
        Assert.True(await store.MarkAsReadAsync(id, readAt, CancellationToken.None));
        Assert.False(await store.MarkAsReadAsync(Guid.NewGuid(), readAt, CancellationToken.None));
        Assert.True(await store.MarkAsReadAsync(id, readAt.AddHours(1), CancellationToken.None));

        InboundMailMessageDetails? readDetails = await store.GetMessageDetailsAsync(id, CancellationToken.None);
        Assert.NotNull(readDetails);
        Assert.Equal(readAt.ToUniversalTime(), readDetails.ReadAtUtc);
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

        Guid id = await store.SaveAsync(message, CancellationToken.None);

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
              and raw_mime = 'raw mime'
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

    private async Task<NpgsqlDataSource> CreateDataSourceAsync() {
        string connectionString = await fixture.CreateIsolatedDatabaseAsync().ConfigureAwait(false);
        var dataSource = NpgsqlDataSource.Create(connectionString);
        await CreateStore(dataSource).EnsureSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        return dataSource;
    }

    private static NpgsqlInboundMailStore CreateStore(NpgsqlDataSource dataSource) =>
        new(dataSource, new DmarcReportParser());

    private static InboundMailMessage CreateMessage(string subject, DateTimeOffset receivedAtUtc) =>
        InboundMailMessage.Receive(
            messageId: null,
            fromAddress: "sender@example.com",
            ["admin@fooddiary.club"],
            subject,
            textBody: null,
            htmlBody: null,
            "raw mime",
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
}
