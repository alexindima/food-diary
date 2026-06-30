using System.Text.Json;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore {
    private const string InsertEmailSql = """
                                          with inserted as (
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
                                              do nothing
                                              returning id, true as inserted
                                          )
                                          select id, inserted
                                          from inserted
                                          union all
                                          select id, false as inserted
                                          from mailrelay_outbound_emails
                                          where idempotency_key = @idempotencyKey
                                            and not exists (select 1 from inserted)
                                          limit 1;
                                          """;
    private const string InsertOutboxSql = """
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
    private const string ClaimDueBatchSql = """
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

    public async Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        ValidateRequest(request);

        var emailId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return await _executor.InTransactionAsync(
            async (connection, transaction, token) => {
                InsertQueuedEmailResult queuedEmail = await InsertQueuedEmailAsync(connection, transaction, request, emailId, now, token).ConfigureAwait(false);
                if (queuedEmail.Inserted) {
                    await InsertOutboxMessageAsync(connection, transaction, outboxId, queuedEmail.Id, now, token).ConfigureAwait(false);
                }

                MailRelayTelemetry.RecordQueueEvent(queuedEmail.Inserted ? "queued" : "duplicate");
                return queuedEmail.Id;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateRequest(RelayEmailMessageRequest request) {
        if (string.IsNullOrWhiteSpace(request.FromAddress)) {
            throw new ArgumentException("Email relay request must contain a sender address.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Subject)) {
            throw new ArgumentException("Email relay request must contain a subject.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.HtmlBody)) {
            throw new ArgumentException("Email relay request must contain an HTML body.", nameof(request));
        }

        if (request.To.Count == 0) {
            throw new InvalidOperationException("Email relay request must contain at least one recipient.");
        }
    }

    private async Task<InsertQueuedEmailResult> InsertQueuedEmailAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        RelayEmailMessageRequest request,
        Guid id,
        DateTimeOffset now,
        CancellationToken cancellationToken) {
        return await _executor.QueryInTransactionAsync(
            connection,
            transaction,
            InsertEmailSql,
            command => {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("fromAddress", request.FromAddress);
                command.Parameters.AddWithValue("fromName", request.FromName);
                command.Parameters.AddWithValue("toRecipientsJson", JsonSerializer.Serialize(request.To, JsonOptions));
                command.Parameters.AddWithValue("subject", request.Subject);
                command.Parameters.AddWithValue("htmlBody", request.HtmlBody);
                command.Parameters.AddWithValue("textBody", (object?)request.TextBody ?? DBNull.Value);
                command.Parameters.AddWithValue("correlationId", (object?)request.CorrelationId ?? DBNull.Value);
                command.Parameters.AddWithValue("idempotencyKey", (object?)request.IdempotencyKey ?? DBNull.Value);
                command.Parameters.AddWithValue("maxAttempts", _queueOptions.MaxAttempts);
                command.Parameters.AddWithValue("availableAtUtc", now);
                command.Parameters.AddWithValue("createdAtUtc", now);
            },
            async (reader, token) => {
                await RequireReturnedRowAsync(reader, token, "Mail relay queue insert did not return an id.").ConfigureAwait(false);
                return new InsertQueuedEmailResult(reader.GetGuid(0), reader.GetBoolean(1));
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertOutboxMessageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid outboxId,
        Guid emailId,
        DateTimeOffset now,
        CancellationToken cancellationToken) {
        var command = new NpgsqlCommand(InsertOutboxSql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("id", outboxId);
            command.Parameters.AddWithValue("emailId", emailId);
            command.Parameters.AddWithValue("availableAtUtc", now);
            command.Parameters.AddWithValue("createdAtUtc", now);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken) {
        return await _executor.QueryAsync(
            ClaimDueBatchSql,
            command => {
                command.Parameters.AddWithValue("batchSize", _queueOptions.BatchSize);
                command.Parameters.AddWithValue("lockTimeoutSeconds", _queueOptions.LockTimeoutSeconds);
            },
            async (reader, token) => {
                var result = new List<QueuedEmailMessage>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    result.Add(MailRelayQueueRowMapper.ReadQueuedEmailMessage(reader));
                }

                return result;
            },
            cancellationToken).ConfigureAwait(false);
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

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("lockTimeoutSeconds", _queueOptions.LockTimeoutSeconds);
            },
            async (reader, token) => {
                if (!await reader.ReadAsync(token).ConfigureAwait(false)) {
                    return null;
                }

                return MailRelayQueueRowMapper.ReadQueuedEmailMessage(reader);
            },
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteStatusCommandAsync(sql, id, cancellationToken).ConfigureAwait(false);
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

        NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("lastError", Truncate($"Suppressed recipient(s): {string.Join(", ", recipients)}", 4000));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
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

        return await _executor.QueryAsync(
            sql,
            configure: null,
            async (reader, token) => await reader.ReadAsync(token).ConfigureAwait(false)
                ? MailRelayQueueRowMapper.ReadQueueStats(reader)
                : new MailRelayQueueStats(0, 0, 0, 0, 0, 0),
            cancellationToken).ConfigureAwait(false);
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

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("id", id);
            },
            async (reader, token) => {
                if (!await reader.ReadAsync(token).ConfigureAwait(false)) {
                    return null;
                }

                string[] recipients = MailRelayQueueRowMapper.ReadMessageRecipients(reader);
                IReadOnlyList<string> suppressedRecipients = await GetSuppressedRecipientsAsync(recipients, token).ConfigureAwait(false);

                return await MailRelayQueueRowMapper.ReadMessageDetailsAsync(reader, suppressedRecipients, token).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) {
        DateTimeOffset? nextAvailableAt = decision.IsTerminalFailure
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

        NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                var command = new NpgsqlCommand(sql, connection, transaction);
                await using (command.ConfigureAwait(false)) {
                    command.Parameters.AddWithValue("id", decision.Id.Value);
                    command.Parameters.AddWithValue("status", decision.Status);
                    command.Parameters.AddWithValue("availableAtUtc", (object?)nextAvailableAt ?? DBNull.Value);
                    command.Parameters.AddWithValue("lastError", Truncate(decision.Error, 4000));
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                if (nextAvailableAt is { } retryAt) {
                    await InsertOutboxMessageAsync(
                        connection,
                        transaction,
                        Guid.NewGuid(),
                        decision.Id.Value,
                        retryAt,
                        cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return nextAvailableAt;
        }
    }

}
