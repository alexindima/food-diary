using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore {
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
                                   updated_at_utc,
                                   claim_token
                               )
                               values (
                                   @id,
                                   @consumerName,
                                   @messageKey,
                                   'processing',
                                   now(),
                                   now(),
                                   now(),
                                   @claimToken
                               )
                               on conflict (consumer_name, message_key)
                               do update set
                                   status = case
                                       when mailrelay_inbox_messages.status = 'failed'
                                         or (mailrelay_inbox_messages.status = 'processing'
                                             and mailrelay_inbox_messages.locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds))
                                       then 'processing'
                                       else mailrelay_inbox_messages.status
                                   end,
                                   locked_at_utc = case
                                       when mailrelay_inbox_messages.status = 'failed'
                                         or (mailrelay_inbox_messages.status = 'processing'
                                             and mailrelay_inbox_messages.locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds))
                                       then now()
                                       else mailrelay_inbox_messages.locked_at_utc
                                   end,
                                   updated_at_utc = case
                                       when mailrelay_inbox_messages.status = 'failed'
                                         or (mailrelay_inbox_messages.status = 'processing'
                                             and mailrelay_inbox_messages.locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds))
                                       then now()
                                       else mailrelay_inbox_messages.updated_at_utc
                                   end,
                                   claim_token = case
                                       when mailrelay_inbox_messages.status = 'failed'
                                         or (mailrelay_inbox_messages.status = 'processing'
                                             and mailrelay_inbox_messages.locked_at_utc <= now() - make_interval(secs => @lockTimeoutSeconds))
                                       then @claimToken
                                       else mailrelay_inbox_messages.claim_token
                                   end
                           returning id, coalesce(claim_token = @claimToken, false) as claimed;
                           """;

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("claimToken", Guid.NewGuid());
                command.Parameters.AddWithValue("consumerName", consumerName);
                command.Parameters.AddWithValue("messageKey", messageKey);
                command.Parameters.AddWithValue("lockTimeoutSeconds", _queueOptions.LockTimeoutSeconds);
            },
            async (reader, token) => {
                await RequireReturnedRowAsync(reader, token, "Inbox claim did not return a row.").ConfigureAwait(false);
                Guid inboxId = reader.GetGuid(0);
                bool claimed = reader.GetBoolean(1);
                MailRelayTelemetry.RecordInboxEvent(claimed ? "claimed" : "duplicate");
                return new MailRelayInboxClaimResult(claimed, inboxId);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_inbox_messages
                           set status = 'processed',
                               processed_at_utc = now(),
                               updated_at_utc = now(),
                               last_error = null,
                               claim_token = null
                           where id = @id;
                           """;

        await ExecuteStatusCommandAsync(sql, id, cancellationToken).ConfigureAwait(false);
        MailRelayTelemetry.RecordInboxEvent("processed");
    }

    public async Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken) {
        const string sql = """
                           update mailrelay_inbox_messages
                           set status = 'failed',
                               updated_at_utc = now(),
                               last_error = @lastError,
                               claim_token = null
                           where id = @id;
                           """;

        NpgsqlConnection connection = await DataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("lastError", Truncate(error, 4000));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                MailRelayTelemetry.RecordInboxEvent("failed");
            }
        }
    }

}
