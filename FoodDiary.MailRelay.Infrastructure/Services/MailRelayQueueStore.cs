using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelayQueueStore(
    NpgsqlDataSource dataSource,
    IOptions<MailRelayQueueOptions> queueOptions) : IMailRelayQueueStore, IMailRelaySchemaInitializer {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly MailRelayQueueOptions _queueOptions = queueOptions.Value;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        const string sql = """
                           create table if not exists mailrelay_outbound_emails (
                               id uuid primary key,
                               status text not null,
                               from_address text not null,
                               from_name text not null,
                               to_recipients_json jsonb not null,
                               subject text not null,
                               html_body text not null,
                               text_body text null,
                               correlation_id text null,
                               idempotency_key text null,
                               attempt_count integer not null default 0,
                               max_attempts integer not null,
                               available_at_utc timestamptz not null,
                               created_at_utc timestamptz not null,
                               locked_at_utc timestamptz null,
                               sent_at_utc timestamptz null,
                               last_error text null
                           );

                           create unique index if not exists ux_mailrelay_outbound_emails_idempotency_key
                               on mailrelay_outbound_emails (idempotency_key)
                               where idempotency_key is not null;

                           create index if not exists ix_mailrelay_outbound_emails_due
                               on mailrelay_outbound_emails (status, available_at_utc, created_at_utc);

                           create index if not exists ix_mailrelay_outbound_emails_processing
                               on mailrelay_outbound_emails (status, locked_at_utc);

                           create table if not exists mailrelay_outbox_messages (
                               id uuid primary key,
                               email_id uuid not null,
                               status text not null,
                               attempt_count integer not null default 0,
                               available_at_utc timestamptz not null,
                               locked_at_utc timestamptz null,
                               published_at_utc timestamptz null,
                               created_at_utc timestamptz not null,
                               last_error text null
                           );

                           create index if not exists ix_mailrelay_outbox_messages_due
                               on mailrelay_outbox_messages (status, available_at_utc, created_at_utc);

                           create table if not exists mailrelay_inbox_messages (
                               id uuid primary key,
                               consumer_name text not null,
                               message_key text not null,
                               status text not null,
                               locked_at_utc timestamptz not null,
                               processed_at_utc timestamptz null,
                               created_at_utc timestamptz not null,
                               updated_at_utc timestamptz not null,
                               last_error text null
                           );

                           create unique index if not exists ux_mailrelay_inbox_messages_consumer_message_key
                               on mailrelay_inbox_messages (consumer_name, message_key);

                           create table if not exists mailrelay_suppressions (
                               email text primary key,
                               reason text not null,
                               source text not null,
                               created_at_utc timestamptz not null,
                               updated_at_utc timestamptz not null,
                               expires_at_utc timestamptz null
                           );

                           create table if not exists mailrelay_delivery_events (
                               id uuid primary key,
                               event_type text not null,
                               email text not null,
                               source text not null,
                               classification text null,
                               provider_message_id text null,
                               reason text null,
                               occurred_at_utc timestamptz not null,
                               created_at_utc timestamptz not null
                           );

                           create index if not exists ix_mailrelay_delivery_events_email_created
                               on mailrelay_delivery_events (email, created_at_utc desc);
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.HtmlBody);
        if (request.To.Count == 0) {
            throw new InvalidOperationException("Email relay request must contain at least one recipient.");
        }

        var id = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        const string emailSql = """
                                insert into mailrelay_outbound_emails (
                                    id,
                                    status,
                                    from_address,
                                    from_name,
                                    to_recipients_json,
                                    subject,
                                    html_body,
                                    text_body,
                                    correlation_id,
                                    idempotency_key,
                                    attempt_count,
                                    max_attempts,
                                    available_at_utc,
                                    created_at_utc
                                )
                                values (
                                    @id,
                                    'pending',
                                    @fromAddress,
                                    @fromName,
                                    cast(@toRecipientsJson as jsonb),
                                    @subject,
                                    @htmlBody,
                                    @textBody,
                                    @correlationId,
                                    @idempotencyKey,
                                    0,
                                    @maxAttempts,
                                    @availableAtUtc,
                                    @createdAtUtc
                                )
                                on conflict (idempotency_key) where idempotency_key is not null
                                do update set idempotency_key = excluded.idempotency_key
                                returning id;
                                """;

        const string outboxSql = """
                                 insert into mailrelay_outbox_messages (
                                     id,
                                     email_id,
                                     status,
                                     attempt_count,
                                     available_at_utc,
                                     created_at_utc
                                 )
                                 values (
                                     @id,
                                     @emailId,
                                     'pending',
                                     0,
                                     @availableAtUtc,
                                     @createdAtUtc
                                 );
                                 """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid emailId;
        await using (var emailCommand = new NpgsqlCommand(emailSql, connection, transaction)) {
            emailCommand.Parameters.AddWithValue("id", id);
            emailCommand.Parameters.AddWithValue("fromAddress", request.FromAddress);
            emailCommand.Parameters.AddWithValue("fromName", request.FromName);
            emailCommand.Parameters.AddWithValue("toRecipientsJson", JsonSerializer.Serialize(request.To, JsonOptions));
            emailCommand.Parameters.AddWithValue("subject", request.Subject);
            emailCommand.Parameters.AddWithValue("htmlBody", request.HtmlBody);
            emailCommand.Parameters.AddWithValue("textBody", (object?)request.TextBody ?? DBNull.Value);
            emailCommand.Parameters.AddWithValue("correlationId", (object?)request.CorrelationId ?? DBNull.Value);
            emailCommand.Parameters.AddWithValue("idempotencyKey", (object?)request.IdempotencyKey ?? DBNull.Value);
            emailCommand.Parameters.AddWithValue("maxAttempts", _queueOptions.MaxAttempts);
            emailCommand.Parameters.AddWithValue("availableAtUtc", now);
            emailCommand.Parameters.AddWithValue("createdAtUtc", now);

            var result = await emailCommand.ExecuteScalarAsync(cancellationToken);
            emailId = result is Guid existingId
                ? existingId
                : throw new InvalidOperationException("Mail relay queue insert did not return an id.");
        }

        await using (var outboxCommand = new NpgsqlCommand(outboxSql, connection, transaction)) {
            outboxCommand.Parameters.AddWithValue("id", outboxId);
            outboxCommand.Parameters.AddWithValue("emailId", emailId);
            outboxCommand.Parameters.AddWithValue("availableAtUtc", now);
            outboxCommand.Parameters.AddWithValue("createdAtUtc", now);
            await outboxCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        MailRelayTelemetry.RecordQueueEvent("queued");
        return emailId;
    }

    public async Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken) {
        const string sql = """
                           with next_batch as (
                               select id
                               from mailrelay_outbound_emails
                               where (
                                   status in ('pending', 'retry') and available_at_utc <= now()
                               ) or (
                                   status = 'processing' and locked_at_utc is not null and locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds)
                               )
                               order by available_at_utc asc, created_at_utc asc
                               limit @batchSize
                               for update skip locked
                           )
                           update mailrelay_outbound_emails queue
                           set status = 'processing',
                               locked_at_utc = now(),
                               attempt_count = queue.attempt_count + 1
                           from next_batch
                           where queue.id = next_batch.id
                           returning
                               queue.id,
                               queue.from_address,
                               queue.from_name,
                               queue.to_recipients_json::text,
                               queue.subject,
                               queue.html_body,
                               queue.text_body,
                               queue.correlation_id,
                               queue.attempt_count,
                               queue.max_attempts;
                           """;

        var result = new List<QueuedEmailMessage>();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("batchSize", _queueOptions.BatchSize);
        command.Parameters.AddWithValue("lockTimeoutSeconds", _queueOptions.LockTimeoutSeconds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            var toRecipientsJson = reader.GetString(3);
            var recipients = JsonSerializer.Deserialize<string[]>(toRecipientsJson, JsonOptions)
                             ?? throw new InvalidOperationException("Mail relay queue row contains invalid recipients JSON.");

            result.Add(new QueuedEmailMessage(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                recipients,
                reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetInt32(8),
                reader.GetInt32(9)));
        }

        return result;
    }

    public async Task<QueuedEmailMessage?> TryClaimMessageByIdAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_outbound_emails
                           set status = 'processing',
                               locked_at_utc = now(),
                               attempt_count = attempt_count + 1
                           where id = @id
                             and (
                                 (status in ('pending', 'retry') and available_at_utc <= now()) or
                                 (status = 'processing' and locked_at_utc is not null and locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds))
                             )
                           returning
                               id,
                               from_address,
                               from_name,
                               to_recipients_json::text,
                               subject,
                               html_body,
                               text_body,
                               correlation_id,
                               attempt_count,
                               max_attempts;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lockTimeoutSeconds", _queueOptions.LockTimeoutSeconds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            return null;
        }

        var toRecipientsJson = reader.GetString(3);
        var recipients = JsonSerializer.Deserialize<string[]>(toRecipientsJson, JsonOptions)
                         ?? throw new InvalidOperationException("Mail relay queue row contains invalid recipients JSON.");

        return new QueuedEmailMessage(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            recipients,
            reader.GetString(4),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.GetInt32(8),
            reader.GetInt32(9));
    }

    public async Task<IReadOnlyList<MailRelayOutboxMessage>> ClaimOutboxBatchAsync(CancellationToken cancellationToken) {
        const string sql = """
                           with next_batch as (
                               select id
                               from mailrelay_outbox_messages
                               where status in ('pending', 'retry') and available_at_utc <= now()
                               order by available_at_utc asc, created_at_utc asc
                               limit @batchSize
                               for update skip locked
                           )
                           update mailrelay_outbox_messages outbox
                           set status = 'processing',
                               locked_at_utc = now(),
                               attempt_count = outbox.attempt_count + 1
                           from next_batch
                           where outbox.id = next_batch.id
                           returning outbox.id, outbox.email_id, outbox.attempt_count;
                           """;

        var result = new List<MailRelayOutboxMessage>();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("batchSize", _queueOptions.BatchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            result.Add(new MailRelayOutboxMessage(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2)));
        }

        return result;
    }

    public async Task MarkOutboxPublishedAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_outbox_messages
                           set status = 'published',
                               published_at_utc = now(),
                               locked_at_utc = null,
                               last_error = null
                           where id = @id;
                           """;

        await ExecuteStatusCommandAsync(sql, id, cancellationToken);
        MailRelayTelemetry.RecordOutboxEvent("published");
    }

    public async Task MarkOutboxFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken) {
        var nextAvailableAt = DateTimeOffset.UtcNow.Add(ComputeBackoff(attemptCount));

        const string sql = """
                           update mailrelay_outbox_messages
                           set status = 'retry',
                               available_at_utc = @availableAtUtc,
                               locked_at_utc = null,
                               last_error = @lastError
                           where id = @id;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("availableAtUtc", nextAvailableAt);
        command.Parameters.AddWithValue("lastError", Truncate(error, 4000));
        await command.ExecuteNonQueryAsync(cancellationToken);
        MailRelayTelemetry.RecordOutboxEvent("retry");
    }

    public async Task<MailRelayInboxClaimResult> TryClaimInboxMessageAsync(
        string consumerName,
        string messageKey,
        CancellationToken cancellationToken) {
        const string sql = """
                           insert into mailrelay_inbox_messages (
                               id,
                               consumer_name,
                               message_key,
                               status,
                               locked_at_utc,
                               created_at_utc,
                               updated_at_utc
                           )
                           values (
                               @id,
                               @consumerName,
                               @messageKey,
                               'processing',
                               now(),
                               now(),
                               now()
                           )
                           on conflict (consumer_name, message_key)
                           do update set
                               status = case
                                   when mailrelay_inbox_messages.status = 'processed' then mailrelay_inbox_messages.status
                                   else 'processing'
                               end,
                               locked_at_utc = case
                                   when mailrelay_inbox_messages.status = 'processed' then mailrelay_inbox_messages.locked_at_utc
                                   else now()
                               end,
                               updated_at_utc = now()
                           returning id, status;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("consumerName", consumerName);
        command.Parameters.AddWithValue("messageKey", messageKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            throw new InvalidOperationException("Inbox claim did not return a row.");
        }

        var inboxId = reader.GetGuid(0);
        var status = reader.GetString(1);
        MailRelayTelemetry.RecordInboxEvent(status == "processing" ? "claimed" : "duplicate");
        return new MailRelayInboxClaimResult(status == "processing", inboxId);
    }

    public async Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_inbox_messages
                           set status = 'processed',
                               processed_at_utc = now(),
                               updated_at_utc = now(),
                               last_error = null
                           where id = @id;
                           """;

        await ExecuteStatusCommandAsync(sql, id, cancellationToken);
        MailRelayTelemetry.RecordInboxEvent("processed");
    }

    public async Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_inbox_messages
                           set status = 'failed',
                               updated_at_utc = now(),
                               last_error = @lastError
                           where id = @id;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lastError", Truncate(error, 4000));
        await command.ExecuteNonQueryAsync(cancellationToken);
        MailRelayTelemetry.RecordInboxEvent("failed");
    }

    public async Task MarkSentAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_outbound_emails
                           set status = 'sent',
                               sent_at_utc = now(),
                               locked_at_utc = null,
                               last_error = null
                           where id = @id;
                           """;

        await ExecuteStatusCommandAsync(sql, id, cancellationToken);
    }

    public async Task MarkSuppressedAsync(
        Guid id,
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_outbound_emails
                           set status = 'suppressed',
                               locked_at_utc = null,
                               last_error = @lastError
                           where id = @id;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lastError", Truncate($"Suppressed recipient(s): {string.Join(", ", recipients)}", 4000));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(
        string? email,
        CancellationToken cancellationToken) {
        const string sql = """
                           select
                               email,
                               reason,
                               source,
                               created_at_utc,
                               updated_at_utc,
                               expires_at_utc
                           from mailrelay_suppressions
                           where @email is null or email = @email
                           order by updated_at_utc desc, email asc;
                           """;

        var entries = new List<MailRelaySuppressionEntry>();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", (object?)NormalizeEmail(email) ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            entries.Add(new MailRelaySuppressionEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return entries;
    }

    public async Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) {
        var normalizedEmail = NormalizeEmail(request.Email)
                              ?? throw new InvalidOperationException("Suppression email must be provided.");

        const string sql = """
                           insert into mailrelay_suppressions (
                               email,
                               reason,
                               source,
                               created_at_utc,
                               updated_at_utc,
                               expires_at_utc
                           )
                           values (
                               @email,
                               @reason,
                               @source,
                               now(),
                               now(),
                               @expiresAtUtc
                           )
                           on conflict (email)
                           do update set
                               reason = excluded.reason,
                               source = excluded.source,
                               updated_at_utc = now(),
                               expires_at_utc = excluded.expires_at_utc;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", normalizedEmail);
        command.Parameters.AddWithValue("reason", request.Reason);
        command.Parameters.AddWithValue("source", request.Source);
        command.Parameters.AddWithValue("expiresAtUtc", (object?)request.ExpiresAtUtc ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken) {
        var normalizedEmail = NormalizeEmail(request.Email)
                              ?? throw new InvalidOperationException("Delivery event email must be provided.");
        var occurredAtUtc = request.OccurredAtUtc ?? DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        const string sql = """
                           insert into mailrelay_delivery_events (
                               id,
                               event_type,
                               email,
                               source,
                               classification,
                               provider_message_id,
                               reason,
                               occurred_at_utc,
                               created_at_utc
                           )
                           values (
                               @id,
                               @eventType,
                               @email,
                               @source,
                               @classification,
                               @providerMessageId,
                               @reason,
                               @occurredAtUtc,
                               now()
                           )
                           returning created_at_utc;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("eventType", request.EventType);
        command.Parameters.AddWithValue("email", normalizedEmail);
        command.Parameters.AddWithValue("source", request.Source);
        command.Parameters.AddWithValue("classification", (object?)request.Classification ?? DBNull.Value);
        command.Parameters.AddWithValue("providerMessageId", (object?)request.ProviderMessageId ?? DBNull.Value);
        command.Parameters.AddWithValue("reason", (object?)request.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("occurredAtUtc", occurredAtUtc);
        var createdAtUtc = (DateTimeOffset)(await command.ExecuteScalarAsync(cancellationToken)
                                            ?? throw new InvalidOperationException("Delivery event insert did not return created_at_utc."));

        return new MailRelayDeliveryEventEntry(
            id,
            request.EventType,
            normalizedEmail,
            request.Source,
            request.Classification,
            request.ProviderMessageId,
            request.Reason,
            occurredAtUtc,
            createdAtUtc);
    }

    public async Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(
        string? email,
        CancellationToken cancellationToken) {
        const string sql = """
                           select
                               id,
                               event_type,
                               email,
                               source,
                               classification,
                               provider_message_id,
                               reason,
                               occurred_at_utc,
                               created_at_utc
                           from mailrelay_delivery_events
                           where @email is null or email = @email
                           order by created_at_utc desc, id desc;
                           """;

        var result = new List<MailRelayDeliveryEventEntry>();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", (object?)NormalizeEmail(email) ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            result.Add(new MailRelayDeliveryEventEntry(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return result;
    }

    public async Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) {
        const string sql = "delete from mailrelay_suppressions where email = @email;";

        var normalizedEmail = NormalizeEmail(email)
                              ?? throw new InvalidOperationException("Suppression email must be provided.");

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("email", normalizedEmail);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken) {
        var normalizedRecipients = recipients
            .Select(NormalizeEmail)
            .Where(static email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedRecipients.Length == 0) {
            return [];
        }

        const string sql = """
                           select email
                           from mailrelay_suppressions
                           where email = any(@emails)
                             and (expires_at_utc is null or expires_at_utc > now())
                           order by email asc;
                           """;

        var emails = new List<string>();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("emails", normalizedRecipients);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            emails.Add(reader.GetString(0));
        }

        return emails;
    }

    public async Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) {
        const string sql = """
                           select
                               count(*) filter (where status = 'pending') as pending_count,
                               count(*) filter (where status = 'retry') as retry_count,
                               count(*) filter (where status = 'processing') as processing_count,
                               count(*) filter (where status = 'sent') as sent_count,
                               count(*) filter (where status = 'failed') as failed_count,
                               count(*) filter (where status = 'suppressed') as suppressed_count
                           from mailrelay_outbound_emails;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            return new MailRelayQueueStats(0, 0, 0, 0, 0, 0);
        }

        return new MailRelayQueueStats(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    public async Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           select
                               id,
                               status,
                               subject,
                               correlation_id,
                               attempt_count,
                               max_attempts,
                               created_at_utc,
                               available_at_utc,
                               locked_at_utc,
                               sent_at_utc,
                               last_error,
                               to_recipients_json::text
                           from mailrelay_outbound_emails
                           where id = @id;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            return null;
        }

        var recipients = JsonSerializer.Deserialize<string[]>(reader.GetString(11), JsonOptions) ?? [];
        var suppressedRecipients = await GetSuppressedRecipientsAsync(recipients, cancellationToken);

        return new MailRelayMessageDetails(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetFieldValue<DateTimeOffset>(6),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            suppressedRecipients);
    }

    public async Task MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) {
        var nextAvailableAt = decision.IsTerminalFailure
            ? (DateTimeOffset?)null
            : DateTimeOffset.UtcNow.Add(ComputeBackoff(decision.AttemptCount));

        const string sql = """
                           update mailrelay_outbound_emails
                           set status = @status,
                               available_at_utc = coalesce(@availableAtUtc, available_at_utc),
                               locked_at_utc = null,
                               last_error = @lastError
                           where id = @id;
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", decision.Id.Value);
        command.Parameters.AddWithValue("status", decision.Status);
        command.Parameters.AddWithValue("availableAtUtc", (object?)nextAvailableAt ?? DBNull.Value);
        command.Parameters.AddWithValue("lastError", Truncate(decision.Error, 4000));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteStatusCommandAsync(string sql, Guid id, CancellationToken cancellationToken) {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private TimeSpan ComputeBackoff(int attemptCount) {
        var exponent = Math.Max(0, attemptCount - 1);
        var retrySeconds = _queueOptions.BaseRetryDelaySeconds * Math.Pow(2, exponent);
        var boundedSeconds = Math.Min(retrySeconds, _queueOptions.MaxRetryDelaySeconds);
        return TimeSpan.FromSeconds(boundedSeconds);
    }

    private static string Truncate(string value, int maxLength) {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeEmail(string? email) {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
