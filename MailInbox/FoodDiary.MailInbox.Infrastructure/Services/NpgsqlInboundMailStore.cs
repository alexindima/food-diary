using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlInboundMailStore(
    NpgsqlDataSource dataSource,
    DmarcReportParser dmarcReportParser,
    IOptions<MailInboxStorageOptions> options,
    TimeProvider timeProvider) : IInboundMailStore, IMailInboxSchemaInitializer {
    private readonly MailInboxStorageOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly MailInboxSchemaMigration[] SchemaMigrations = [
        new(
            "202606140001_create_mailinbox_messages",
            """
            create table if not exists mailinbox_messages (
                id uuid primary key,
                message_id text null,
                from_address text null,
                to_recipients_json jsonb not null,
                subject text null,
                text_body text null,
                html_body text null,
                raw_mime text not null,
                status text not null,
                received_at_utc timestamptz not null
            );

            create index if not exists ix_mailinbox_messages_received_at_utc
                on mailinbox_messages (received_at_utc desc);
            """),
        new(
            "202606140002_add_mailinbox_message_read_at_utc",
            """
            alter table mailinbox_messages
                add column if not exists read_at_utc timestamptz null;

            create index if not exists ix_mailinbox_messages_unread_received_at_utc
                on mailinbox_messages (received_at_utc desc)
                where read_at_utc is null;
            """),
        new(
            "202608170001_harden_mailinbox_ingestion",
            """
            alter table mailinbox_messages
                alter column raw_mime type bytea using convert_to(raw_mime, 'UTF8'),
                alter column raw_mime drop not null;

            alter table mailinbox_messages
                add column if not exists ingestion_key text null,
                add column if not exists deduplication_bucket_utc timestamptz null,
                add column if not exists raw_size_bytes bigint not null default 0,
                add column if not exists content_purged_at_utc timestamptz null;

            update mailinbox_messages
            set ingestion_key = id::text,
                deduplication_bucket_utc = date_trunc('day', received_at_utc),
                raw_size_bytes = coalesce(octet_length(raw_mime), 0)
            where ingestion_key is null
               or deduplication_bucket_utc is null;

            alter table mailinbox_messages
                alter column ingestion_key set not null,
                alter column deduplication_bucket_utc set not null;

            create unique index if not exists ux_mailinbox_messages_ingestion_window
                on mailinbox_messages (ingestion_key, deduplication_bucket_utc);

            create index if not exists ix_mailinbox_messages_content_retention
                on mailinbox_messages (received_at_utc)
                where content_purged_at_utc is null;

            create table if not exists mailinbox_daily_ingestion_usage (
                usage_date date primary key,
                message_count bigint not null,
                raw_bytes bigint not null
            );
            """),
    ];
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            await EnsureMigrationTableAsync(connection, cancellationToken).ConfigureAwait(false);

            foreach (MailInboxSchemaMigration migration in SchemaMigrations) {
                if (await IsMigrationAppliedAsync(connection, migration.Name, cancellationToken).ConfigureAwait(false)) {
                    continue;
                }

                await ApplyMigrationAsync(connection, migration, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<InboundMailSaveResult> SaveAsync(
        InboundMailMessage message,
        CancellationToken cancellationToken) {
        const string sql = """
                           insert into mailinbox_messages (
                               id,
                               message_id,
                               from_address,
                               to_recipients_json,
                               subject,
                               text_body,
                               html_body,
                               raw_mime,
                               ingestion_key,
                               deduplication_bucket_utc,
                               raw_size_bytes,
                               status,
                               received_at_utc)
                           values (
                               @id,
                               @message_id,
                               @from_address,
                               @to_recipients_json::jsonb,
                               @subject,
                               @text_body,
                               @html_body,
                               @raw_mime,
                               @ingestion_key,
                               @deduplication_bucket_utc,
                               @raw_size_bytes,
                               @status,
                               @received_at_utc)
                           on conflict (ingestion_key, deduplication_bucket_utc)
                           do update set ingestion_key = excluded.ingestion_key
                           returning id;
                           """;

        Guid id = message.Id.Value;
        string ingestionKey = CalculateIngestionKey(message);
        DateTimeOffset deduplicationBucketUtc = GetDeduplicationBucket(message.ReceivedAtUtc);
        long rawSizeBytes = message.RawMimeBytes.Length;
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                var command = new NpgsqlCommand(sql, connection, transaction);
                await using (command.ConfigureAwait(false)) {
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithNullableValue("message_id", message.MessageId);
                    command.Parameters.AddWithNullableValue("from_address", message.FromAddress);
                    command.Parameters.AddWithValue("to_recipients_json", JsonSerializer.Serialize(message.ToRecipients, JsonOptions));
                    command.Parameters.AddWithNullableValue("subject", message.Subject);
                    command.Parameters.AddWithNullableValue("text_body", message.TextBody);
                    command.Parameters.AddWithNullableValue("html_body", message.HtmlBody);
                    command.Parameters.Add(new NpgsqlParameter("raw_mime", NpgsqlDbType.Bytea) {
                        Value = message.RawMimeBytes,
                    });
                    command.Parameters.AddWithValue("ingestion_key", ingestionKey);
                    command.Parameters.AddWithValue("deduplication_bucket_utc", deduplicationBucketUtc);
                    command.Parameters.AddWithValue("raw_size_bytes", rawSizeBytes);
                    command.Parameters.AddWithValue("status", message.Status.Value);
                    command.Parameters.AddWithValue("received_at_utc", message.ReceivedAtUtc);
                    id = (Guid)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                                ?? throw new InvalidOperationException("MailInbox insert did not return an identifier."));
                }

                bool wasDuplicate = id != message.Id.Value;
                if (!wasDuplicate && !await TryConsumeDailyQuotaAsync(
                        connection,
                        transaction,
                        message.ReceivedAtUtc,
                        rawSizeBytes,
                        cancellationToken).ConfigureAwait(false)) {
                    throw new InboundMailStorageQuotaExceededException();
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new InboundMailSaveResult(id, wasDuplicate);
            }
        }
    }

    public async Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
        const string sql = """
                           select id, from_address, to_recipients_json::text, subject, status, read_at_utc, received_at_utc
                           from mailinbox_messages
                           order by received_at_utc desc
                           limit @limit;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var messages = new List<InboundMailMessageSummary>();
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("limit", limit);
                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false)) {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                        IReadOnlyList<string> recipients = DeserializeRecipients(reader.GetString(2));
                        string? subject = reader.GetNullableString(3);
                        messages.Add(new InboundMailMessageSummary(
                            reader.GetGuid(0),
                            reader.GetNullableString(1),
                            recipients,
                            subject,
                            GetCategory(recipients, subject),
                            reader.GetString(4),
                            await reader.GetNullableDateTimeOffsetAsync(5, cancellationToken).ConfigureAwait(false),
                            await reader.GetFieldValueAsync<DateTimeOffset>(6, cancellationToken).ConfigureAwait(false)));
                    }
                }
            }
        }

        return messages;
    }

    public async Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           select id, message_id, from_address, to_recipients_json::text, subject, text_body, html_body, raw_mime, status, read_at_utc, received_at_utc, content_purged_at_utc
                           from mailinbox_messages
                           where id = @id;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        InboundMailMessageDetails? details = null;
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false)) {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                        return null;
                    }

                    IReadOnlyList<string> recipients = DeserializeRecipients(reader.GetString(3));
                    string? subject = reader.GetNullableString(4);
                    byte[]? rawMimeBytes = await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false)
                        ? null
                        : await reader.GetFieldValueAsync<byte[]>(7, cancellationToken).ConfigureAwait(false);
                    string? rawMime = rawMimeBytes is null ? null : Encoding.UTF8.GetString(rawMimeBytes);
                    DmarcReportPreview? dmarcReport = rawMime is null ? null : dmarcReportParser.TryParse(rawMime);

                    details = new InboundMailMessageDetails(
                        reader.GetGuid(0),
                        reader.GetNullableString(1),
                        reader.GetNullableString(2),
                        recipients,
                        subject,
                        reader.GetNullableString(5),
                        reader.GetNullableString(6),
                        rawMime,
                        dmarcReport is null ? GetCategory(recipients, subject) : InboundMailMessageCategories.DmarcReport,
                        dmarcReport,
                        reader.GetString(8),
                        await reader.GetNullableDateTimeOffsetAsync(9, cancellationToken).ConfigureAwait(false),
                        await reader.GetFieldValueAsync<DateTimeOffset>(10, cancellationToken).ConfigureAwait(false),
                        await reader.GetNullableDateTimeOffsetAsync(11, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        return details;
    }

    public async Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) {
        const string sql = """
                           update mailinbox_messages
                           set read_at_utc = coalesce(read_at_utc, @read_at_utc)
                           where id = @id;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        int affectedRows;
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("read_at_utc", readAtUtc.ToUniversalTime());
                affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return affectedRows > 0;
    }

    public async Task<InboundMailRetentionResult> PurgeExpiredAsync(
        DateTimeOffset contentCutoffUtc,
        DateTimeOffset metadataCutoffUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        if (metadataCutoffUtc > contentCutoffUtc) {
            throw new ArgumentException("Metadata cutoff must not be newer than the content cutoff.", nameof(metadataCutoffUtc));
        }

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                int metadataDeletedCount = await DeleteExpiredMetadataAsync(
                    connection,
                    transaction,
                    metadataCutoffUtc,
                    batchSize,
                    cancellationToken).ConfigureAwait(false);
                int contentPurgedCount = await PurgeExpiredContentAsync(
                    connection,
                    transaction,
                    contentCutoffUtc,
                    batchSize,
                    cancellationToken).ConfigureAwait(false);
                await DeleteExpiredUsageAsync(
                    connection,
                    transaction,
                    DateOnly.FromDateTime(metadataCutoffUtc.UtcDateTime),
                    cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new InboundMailRetentionResult(contentPurgedCount, metadataDeletedCount);
            }
        }
    }

    private async Task<bool> TryConsumeDailyQuotaAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset receivedAtUtc,
        long rawSizeBytes,
        CancellationToken cancellationToken) {
        const string sql = """
                           insert into mailinbox_daily_ingestion_usage (usage_date, message_count, raw_bytes)
                           values (@usage_date, 1, @raw_bytes)
                           on conflict (usage_date) do update
                           set message_count = mailinbox_daily_ingestion_usage.message_count + 1,
                               raw_bytes = mailinbox_daily_ingestion_usage.raw_bytes + excluded.raw_bytes
                           where mailinbox_daily_ingestion_usage.message_count < @max_messages
                             and mailinbox_daily_ingestion_usage.raw_bytes + excluded.raw_bytes <= @max_raw_bytes
                           returning true;
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("usage_date", DateOnly.FromDateTime(receivedAtUtc.UtcDateTime));
            command.Parameters.AddWithValue("raw_bytes", rawSizeBytes);
            command.Parameters.AddWithValue("max_messages", _options.MaxMessagesPerDay);
            command.Parameters.AddWithValue("max_raw_bytes", _options.MaxRawBytesPerDay);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
        }
    }

    private static async Task<int> DeleteExpiredMetadataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        const string sql = """
                           delete from mailinbox_messages
                           where id in (
                               select id
                               from mailinbox_messages
                               where received_at_utc < @cutoff_utc
                               order by received_at_utc
                               for update skip locked
                               limit @batch_size
                           );
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("cutoff_utc", cutoffUtc.ToUniversalTime());
            command.Parameters.AddWithValue("batch_size", batchSize);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int> PurgeExpiredContentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        const string sql = """
                           with candidates as (
                               select id
                               from mailinbox_messages
                               where received_at_utc < @cutoff_utc
                                 and content_purged_at_utc is null
                               order by received_at_utc
                               for update skip locked
                               limit @batch_size
                           )
                           update mailinbox_messages as message
                           set text_body = null,
                               html_body = null,
                               raw_mime = null,
                               content_purged_at_utc = @purged_at_utc
                           from candidates
                           where message.id = candidates.id;
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("cutoff_utc", cutoffUtc.ToUniversalTime());
            command.Parameters.AddWithValue("batch_size", batchSize);
            command.Parameters.AddWithValue("purged_at_utc", timeProvider.GetUtcNow());
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DeleteExpiredUsageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateOnly cutoffDate,
        CancellationToken cancellationToken) {
        const string sql = "delete from mailinbox_daily_ingestion_usage where usage_date < @cutoff_date;";
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("cutoff_date", cutoffDate);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private string CalculateIngestionKey(InboundMailMessage message) {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendHashValue(hash, message.FromAddress ?? string.Empty);
        foreach (string recipient in message.ToRecipients
                     .Select(static value => value.Trim().ToLowerInvariant())
                     .Order(StringComparer.Ordinal)) {
            AppendHashValue(hash, recipient);
        }

        hash.AppendData(message.RawMimeBytes.Span);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private DateTimeOffset GetDeduplicationBucket(DateTimeOffset receivedAtUtc) {
        long windowTicks = _options.DeduplicationWindow.Ticks;
        long bucketTicks = receivedAtUtc.ToUniversalTime().Ticks / windowTicks * windowTicks;
        return new DateTimeOffset(bucketTicks, TimeSpan.Zero);
    }

    private static void AppendHashValue(IncrementalHash hash, string value) {
        hash.AppendData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        hash.AppendData([0]);
    }

    private static IReadOnlyList<string> DeserializeRecipients(string value) {
        return JsonSerializer.Deserialize<string[]>(value, JsonOptions) ?? [];
    }

    private static string GetCategory(IReadOnlyList<string> recipients, string? subject) {
        if (recipients.Any(static recipient => recipient.Equals("dmarc@fooddiary.club", StringComparison.OrdinalIgnoreCase)) ||
            subject?.Contains("DMARC", StringComparison.OrdinalIgnoreCase) == true ||
            subject?.Contains("Report Domain:", StringComparison.OrdinalIgnoreCase) == true) {
            return InboundMailMessageCategories.DmarcReport;
        }

        return InboundMailMessageCategories.General;
    }

    private static async Task EnsureMigrationTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken) {
        const string sql = """
                           create table if not exists mailinbox_schema_migrations (
                               name text primary key,
                               applied_at_utc timestamptz not null
                           );
                           """;

        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<bool> IsMigrationAppliedAsync(
        NpgsqlConnection connection,
        string name,
        CancellationToken cancellationToken) {
        const string sql = """
                           select exists (
                               select 1
                               from mailinbox_schema_migrations
                               where name = @name
                           );
                           """;

        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("name", name);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is true;
        }
    }

    private async Task ApplyMigrationAsync(
        NpgsqlConnection connection,
        MailInboxSchemaMigration migration,
        CancellationToken cancellationToken) {
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false)) {
            var migrationCommand = new NpgsqlCommand(migration.Sql, connection, transaction);
            await using (migrationCommand.ConfigureAwait(false)) {
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            const string insertSql = """
                                     insert into mailinbox_schema_migrations (name, applied_at_utc)
                                     values (@name, @applied_at_utc)
                                     on conflict (name) do nothing;
                                     """;
            var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            await using (insertCommand.ConfigureAwait(false)) {
                insertCommand.Parameters.AddWithValue("name", migration.Name);
                insertCommand.Parameters.AddWithValue("applied_at_utc", timeProvider.GetUtcNow());
                await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed record MailInboxSchemaMigration(string Name, string Sql);
}
