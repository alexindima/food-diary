
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore {
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

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("batchSize", _queueOptions.BatchSize);
            },
            async (reader, token) => {
                var result = new List<MailRelayOutboxMessage>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    result.Add(MailRelayQueueRowMapper.ReadOutboxMessage(reader));
                }

                return result;
            },
            cancellationToken).ConfigureAwait(false);
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

        await ExecuteStatusCommandAsync(sql, id, cancellationToken).ConfigureAwait(false);
        MailRelayTelemetry.RecordOutboxEvent("published");
    }

    public async Task MarkOutboxFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken) {
        bool shouldRetry = attemptCount < _queueOptions.MaxAttempts;
        DateTimeOffset? nextAvailableAt = shouldRetry
            ? DateTimeOffset.UtcNow.Add(ComputeBackoff(attemptCount))
            : null;

        const string sql = """
                           update mailrelay_outbox_messages
                           set status = @status,
                               available_at_utc = coalesce(@availableAtUtc, available_at_utc),
                               locked_at_utc = null,
                               last_error = @lastError
                           where id = @id;
                           """;

        NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("status", shouldRetry ? "retry" : "failed");
                command.Parameters.AddWithValue("availableAtUtc", (object?)nextAvailableAt ?? DBNull.Value);
                command.Parameters.AddWithValue("lastError", Truncate(error, 4000));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                MailRelayTelemetry.RecordOutboxEvent(shouldRetry ? "retry" : "failed");
            }
        }
    }

}
