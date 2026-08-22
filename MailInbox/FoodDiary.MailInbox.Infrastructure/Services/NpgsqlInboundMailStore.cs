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
    IMailInboxDmarcReportParser dmarcReportParser,
    IOptions<MailInboxStorageOptions> options,
    TimeProvider timeProvider) : IInboundMailStore, IMailInboxSchemaInitializer, IDisposable {
    private const long SchemaMigrationLockKey = 5_564_833_284_657_606_737;
    private readonly MailInboxStorageOptions _options = options.Value;
    private readonly SemaphoreSlim _messageDetailReadSlots = new(
        options.Value.MaxConcurrentMessageDetailReads,
        options.Value.MaxConcurrentMessageDetailReads);
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
        new(
            "202608190001_add_sliding_dedup_index",
            """
            create index if not exists ix_mailinbox_messages_ingestion_received_at_utc
                on mailinbox_messages (ingestion_key, received_at_utc desc);
            """),
        new(
            "202608190002_bound_persisted_mail_metadata",
            """
            update mailinbox_messages
            set message_id = left(message_id, 998),
                from_address = left(from_address, 320),
                subject = left(subject, 998)
            where char_length(message_id) > 998
               or char_length(from_address) > 320
               or char_length(subject) > 998;

            create or replace function mailinbox_recipients_within_limits(recipients pg_catalog.jsonb)
            returns boolean
            language sql
            immutable
            parallel safe
            strict
            as $function$
                select case
                    when pg_catalog.jsonb_typeof(recipients) <> 'array' then false
                    else pg_catalog.jsonb_array_length(recipients) between 1 and 100
                         and not exists (
                             select 1
                             from pg_catalog.jsonb_array_elements(recipients) as recipient(value)
                             where pg_catalog.jsonb_typeof(recipient.value) <> 'string'
                                or btrim(recipient.value #>> '{}') = ''
                                or char_length(recipient.value #>> '{}') > 320
                         )
                end;
            $function$;

            update mailinbox_messages
            set to_recipients_json = case
                when pg_catalog.jsonb_typeof(to_recipients_json) = 'array' then coalesce((
                    select pg_catalog.jsonb_agg(left(recipient.value, 320) order by recipient.ordinality)
                    from pg_catalog.jsonb_array_elements_text(to_recipients_json)
                         with ordinality as recipient(value, ordinality)
                    where recipient.ordinality <= 100
                      and btrim(recipient.value) <> ''
                ), '["unknown@invalid"]'::jsonb)
                else '["unknown@invalid"]'::jsonb
            end
            where not mailinbox_recipients_within_limits(to_recipients_json);

            do $migration$
            begin
                if not exists (
                    select 1 from pg_constraint
                    where conname = 'ck_mailinbox_messages_message_id_length'
                      and conrelid = 'mailinbox_messages'::regclass
                ) then
                    alter table mailinbox_messages
                        add constraint ck_mailinbox_messages_message_id_length
                        check (message_id is null or char_length(message_id) <= 998);
                end if;

                if not exists (
                    select 1 from pg_constraint
                    where conname = 'ck_mailinbox_messages_from_address_length'
                      and conrelid = 'mailinbox_messages'::regclass
                ) then
                    alter table mailinbox_messages
                        add constraint ck_mailinbox_messages_from_address_length
                        check (from_address is null or char_length(from_address) <= 320);
                end if;

                if not exists (
                    select 1 from pg_constraint
                    where conname = 'ck_mailinbox_messages_subject_length'
                      and conrelid = 'mailinbox_messages'::regclass
                ) then
                    alter table mailinbox_messages
                        add constraint ck_mailinbox_messages_subject_length
                        check (subject is null or char_length(subject) <= 998);
                end if;

                if not exists (
                    select 1 from pg_constraint
                    where conname = 'ck_mailinbox_messages_recipients_limits'
                      and conrelid = 'mailinbox_messages'::regclass
                ) then
                    alter table mailinbox_messages
                        add constraint ck_mailinbox_messages_recipients_limits
                        check (mailinbox_recipients_within_limits(to_recipients_json));
                end if;
            end
            $migration$;
            """),
        new(
            "202608200001_reserve_trusted_ingestion_capacity",
            """
            alter table mailinbox_daily_ingestion_usage
                add column if not exists untrusted_message_count bigint not null default 0,
                add column if not exists untrusted_raw_bytes bigint not null default 0;
            """),
        new(
            "202608200002_add_mail_authentication_provenance",
            """
            alter table mailinbox_messages
                add column if not exists envelope_from_address text null,
                add column if not exists is_trusted_relay boolean not null default false;

            do $migration$
            begin
                if not exists (
                    select 1 from pg_constraint
                    where conname = 'ck_mailinbox_messages_envelope_from_address_length'
                      and conrelid = 'mailinbox_messages'::regclass
                ) then
                    alter table mailinbox_messages
                        add constraint ck_mailinbox_messages_envelope_from_address_length
                        check (envelope_from_address is null or char_length(envelope_from_address) <= 320);
                end if;
            end
            $migration$;
            """),
    ];
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            await AcquireSchemaMigrationLockAsync(connection, cancellationToken).ConfigureAwait(false);
            try {
                await EnsureMigrationTableAsync(connection, cancellationToken).ConfigureAwait(false);

                foreach (MailInboxSchemaMigration migration in SchemaMigrations) {
                    if (await IsMigrationAppliedAsync(connection, migration.Name, cancellationToken).ConfigureAwait(false)) {
                        continue;
                    }

                    await ApplyMigrationAsync(connection, migration, cancellationToken).ConfigureAwait(false);
                }
            } finally {
                await ReleaseSchemaMigrationLockAsync(connection, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public Task<InboundMailSaveResult> SaveAsync(
        InboundMailMessage message,
        CancellationToken cancellationToken) =>
        SaveAsync(message, InboundMailAdmission.Untrusted, cancellationToken);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<InboundMailSaveResult> SaveAsync(
        InboundMailMessage message,
        InboundMailAdmission admission,
        CancellationToken cancellationToken) {
        MailInboxStoredMessageLimits.ThrowIfInvalid(message, admission);
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
                               envelope_from_address,
                               is_trusted_relay,
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
                               @envelope_from_address,
                               @is_trusted_relay,
                               @status,
                               @received_at_utc)
                           on conflict (ingestion_key, deduplication_bucket_utc)
                           do update set ingestion_key = excluded.ingestion_key
                           returning id;
                           """;

        Guid id = message.Id.Value;
        string recipientsJson = JsonSerializer.Serialize(message.ToRecipients, JsonOptions);
        string ingestionKey = CalculateIngestionKey(message, admission);
        DateTimeOffset deduplicationBucketUtc = GetDeduplicationBucket(message.ReceivedAtUtc);
        long rawSizeBytes = message.RawMimeBytes.Length;
        long accountedSizeBytes = CalculateAccountedSizeBytes(message, recipientsJson, admission);
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                await AcquireIngestionLockAsync(
                    connection,
                    transaction,
                    ingestionKey,
                    cancellationToken).ConfigureAwait(false);
                Guid? duplicateId = await FindRecentDuplicateAsync(
                    connection,
                    transaction,
                    ingestionKey,
                    message.ReceivedAtUtc - _options.DeduplicationWindow,
                    cancellationToken).ConfigureAwait(false);
                if (duplicateId is not null) {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return new InboundMailSaveResult(duplicateId.Value, WasDuplicate: true);
                }

                var command = new NpgsqlCommand(sql, connection, transaction);
                await using (command.ConfigureAwait(false)) {
                    command.Parameters.AddWithValue("id", id);
                    command.Parameters.AddWithNullableValue("message_id", message.MessageId);
                    command.Parameters.AddWithNullableValue("from_address", message.FromAddress);
                    command.Parameters.AddWithValue("to_recipients_json", recipientsJson);
                    command.Parameters.AddWithNullableValue("subject", message.Subject);
                    command.Parameters.AddWithNullableValue("text_body", message.TextBody);
                    command.Parameters.AddWithNullableValue("html_body", message.HtmlBody);
                    command.Parameters.Add(new NpgsqlParameter("raw_mime", NpgsqlDbType.Bytea) {
                        Value = message.RawMimeBytes,
                    });
                    command.Parameters.AddWithValue("ingestion_key", ingestionKey);
                    command.Parameters.AddWithValue("deduplication_bucket_utc", deduplicationBucketUtc);
                    command.Parameters.AddWithValue("raw_size_bytes", rawSizeBytes);
                    command.Parameters.AddWithNullableValue("envelope_from_address", admission.EnvelopeFromAddress);
                    command.Parameters.AddWithValue("is_trusted_relay", admission.IsTrustedRelay);
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
                        accountedSizeBytes,
                        admission,
                        cancellationToken).ConfigureAwait(false)) {
                    throw new InboundMailStorageQuotaExceededException();
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new InboundMailSaveResult(id, wasDuplicate);
            }
        }
    }

    private static async Task AcquireIngestionLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string ingestionKey,
        CancellationToken cancellationToken) {
        const string sql = "select pg_advisory_xact_lock(hashtextextended(@ingestion_key, 0));";
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("ingestion_key", ingestionKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Guid?> FindRecentDuplicateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string ingestionKey,
        DateTimeOffset windowStartUtc,
        CancellationToken cancellationToken) {
        const string sql = """
                           select id
                           from mailinbox_messages
                           where ingestion_key = @ingestion_key
                             and received_at_utc >= @window_start_utc
                           order by received_at_utc desc
                           limit 1;
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("ingestion_key", ingestionKey);
            command.Parameters.AddWithValue("window_start_utc", windowStartUtc.ToUniversalTime());
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is Guid id ? id : null;
        }
    }

    public async Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
        const string sql = """
                           select id, from_address, to_recipients_json::text, subject, status, read_at_utc, received_at_utc,
                               envelope_from_address, is_trusted_relay
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
                             await reader.GetFieldValueAsync<DateTimeOffset>(6, cancellationToken).ConfigureAwait(false),
                             reader.GetNullableString(7),
                             await reader.GetFieldValueAsync<bool>(8, cancellationToken).ConfigureAwait(false)));
                    }
                }
            }
        }

        return messages;
    }

    public async Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        await _messageDetailReadSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            return await GetMessageDetailsCoreAsync(id, cancellationToken).ConfigureAwait(false);
        } finally {
            _messageDetailReadSlots.Release();
        }
    }

    private async Task<InboundMailMessageDetails?> GetMessageDetailsCoreAsync(
        Guid id,
        CancellationToken cancellationToken) {
        const string sql = """
                           select id, message_id, from_address, to_recipients_json::text, subject, text_body, html_body, raw_mime, status, read_at_utc, received_at_utc, content_purged_at_utc,
                               envelope_from_address, is_trusted_relay
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
                    DmarcReportPreview? dmarcReport = rawMime is null
                        ? null
                        : dmarcReportParser.TryParse(rawMime, cancellationToken);

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
                         await reader.GetNullableDateTimeOffsetAsync(11, cancellationToken).ConfigureAwait(false),
                         reader.GetNullableString(12),
                         await reader.GetFieldValueAsync<bool>(13, cancellationToken).ConfigureAwait(false));
                }
            }
        }

        return details;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) {
        const string sql = """
                           with existing as materialized (
                               select id
                               from mailinbox_messages
                               where id = @id
                           ),
                           updated as (
                               update mailinbox_messages
                               set read_at_utc = @read_at_utc
                               where id = @id
                                 and read_at_utc is null
                               returning id
                           )
                           select exists(select 1 from existing)
                               or exists(select 1 from updated);
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("read_at_utc", readAtUtc.ToUniversalTime());
                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result is true;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
        long accountedSizeBytes,
        InboundMailAdmission admission,
        CancellationToken cancellationToken) {
        const string sql = """
                           insert into mailinbox_daily_ingestion_usage (
                               usage_date,
                               message_count,
                               raw_bytes,
                               untrusted_message_count,
                               untrusted_raw_bytes)
                           select
                               @usage_date,
                               1,
                               @accounted_bytes,
                               @untrusted_message_increment,
                               @untrusted_bytes_increment
                           where @accounted_bytes <= @max_raw_bytes
                             and (@is_trusted_relay
                                  or (@untrusted_message_increment <= @max_untrusted_messages
                                      and @untrusted_bytes_increment <= @max_untrusted_raw_bytes))
                           on conflict (usage_date) do update
                           set message_count = mailinbox_daily_ingestion_usage.message_count + 1,
                               raw_bytes = mailinbox_daily_ingestion_usage.raw_bytes + excluded.raw_bytes,
                               untrusted_message_count = mailinbox_daily_ingestion_usage.untrusted_message_count + excluded.untrusted_message_count,
                               untrusted_raw_bytes = mailinbox_daily_ingestion_usage.untrusted_raw_bytes + excluded.untrusted_raw_bytes
                           where mailinbox_daily_ingestion_usage.message_count < @max_messages
                             and mailinbox_daily_ingestion_usage.raw_bytes + excluded.raw_bytes <= @max_raw_bytes
                             and (@is_trusted_relay
                                  or (mailinbox_daily_ingestion_usage.untrusted_message_count < @max_untrusted_messages
                                      and mailinbox_daily_ingestion_usage.untrusted_raw_bytes + excluded.untrusted_raw_bytes <= @max_untrusted_raw_bytes))
                           returning true;
                           """;
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("usage_date", DateOnly.FromDateTime(receivedAtUtc.UtcDateTime));
            command.Parameters.AddWithValue("accounted_bytes", accountedSizeBytes);
            command.Parameters.AddWithValue("max_messages", _options.MaxMessagesPerDay);
            command.Parameters.AddWithValue("max_raw_bytes", _options.MaxRawBytesPerDay);
            command.Parameters.AddWithValue("is_trusted_relay", admission.IsTrustedRelay);
            command.Parameters.AddWithValue("untrusted_message_increment", admission.IsTrustedRelay ? 0 : 1);
            command.Parameters.AddWithValue("untrusted_bytes_increment", admission.IsTrustedRelay ? 0L : accountedSizeBytes);
            command.Parameters.AddWithValue("max_untrusted_messages", _options.GetMaxUntrustedMessagesPerDay());
            command.Parameters.AddWithValue("max_untrusted_raw_bytes", _options.GetMaxUntrustedRawBytesPerDay());
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

    private string CalculateIngestionKey(InboundMailMessage message, InboundMailAdmission admission) {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendHashValue(hash, admission.EnvelopeFromAddress ?? string.Empty);
        AppendHashValue(hash, message.FromAddress ?? string.Empty);
        foreach (string recipient in message.ToRecipients
                     .Select(static value => value.Trim().ToLowerInvariant())
                     .Order(StringComparer.Ordinal)) {
            AppendHashValue(hash, recipient);
        }

        hash.AppendData(message.RawMimeBytes.Span);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static long CalculateAccountedSizeBytes(
        InboundMailMessage message,
        string recipientsJson,
        InboundMailAdmission admission) {
        return checked(
            message.RawMimeBytes.Length +
            GetUtf8ByteCount(message.MessageId) +
            GetUtf8ByteCount(message.FromAddress) +
            GetUtf8ByteCount(admission.EnvelopeFromAddress) +
            Encoding.UTF8.GetByteCount(recipientsJson) +
            GetUtf8ByteCount(message.Subject) +
            GetUtf8ByteCount(message.TextBody) +
            GetUtf8ByteCount(message.HtmlBody) +
            Encoding.UTF8.GetByteCount(message.Status.Value));
    }

    private static int GetUtf8ByteCount(string? value) =>
        value is null ? 0 : Encoding.UTF8.GetByteCount(value);

    public void Dispose() => _messageDetailReadSlots.Dispose();

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

    private static async Task AcquireSchemaMigrationLockAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = "select pg_advisory_lock(@lock_key);";
        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("lock_key", SchemaMigrationLockKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task ReleaseSchemaMigrationLockAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        if (connection.State != System.Data.ConnectionState.Open) {
            return;
        }

        const string sql = "select pg_advisory_unlock(@lock_key);";
        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("lock_key", SchemaMigrationLockKey);
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
