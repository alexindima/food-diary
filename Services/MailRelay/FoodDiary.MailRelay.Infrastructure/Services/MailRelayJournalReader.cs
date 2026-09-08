using System.Text.Json;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelayJournalReader(NpgsqlDataSource dataSource) : IMailRelayJournalReader {
    public async Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null) {
        const string filter = """
            from mailrelay_outbound_emails
            where (@from_utc is null or created_at_utc >= @from_utc)
              and (@to_utc is null or created_at_utc < @to_utc)
              and (@id is null or id = @id)
              and (@correlation = '' or correlation_id = @correlation)
              and (@purpose = '' or purpose = @purpose)
              and (@recipient = '' or exists (
                select 1 from
                jsonb_array_elements_text(to_recipients_json) r where lower(r) = lower(@recipient)))
            """;
        const string statusFilter = " and (@status = '' or status = @status)";
        // Fail closed for historical/unclassified mail and credentials. Never return raw HTML or provider errors.
        const string projection = """
            select id, status, purpose, from_address, to_recipients_json::text,
                case when purpose = 'bug_report_received' then subject else '' end,
                created_at_utc, sent_at_utc, attempt_count, max_attempts, correlation_id,
                case when purpose = 'bug_report_received' then text_body else null end,
                reply_to, in_reply_to
            """;
        var executor = new MailRelayPostgresExecutor(dataSource);
        return await executor.QueryAsync("select count(*) " + filter + statusFilter + ";" + projection + " " + filter + statusFilter + " order by created_at_utc desc, id desc limit @limit offset @offset;" + "select status, count(*) " + filter + " group by status;",
            command => {
                command.Parameters.AddWithValue("from_utc", NpgsqlTypes.NpgsqlDbType.TimestampTz, (object?)fromUtc?.ToUniversalTime() ?? DBNull.Value);
                command.Parameters.AddWithValue("to_utc", NpgsqlTypes.NpgsqlDbType.TimestampTz, (object?)toUtc?.ToUniversalTime() ?? DBNull.Value);
                command.Parameters.AddWithValue("id", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)id ?? DBNull.Value);
                command.Parameters.AddWithValue("correlation", correlationId?.Trim() ?? "");
                command.Parameters.AddWithValue("purpose", purpose?.Trim() ?? "");
                command.Parameters.AddWithValue("status", status?.Trim() ?? "");
                command.Parameters.AddWithValue("recipient", recipient?.Trim() ?? "");
                command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 100));
                command.Parameters.AddWithValue("offset", (Math.Clamp(page, 1, 10000) - 1) * Math.Clamp(limit, 1, 100));
            }, async (reader, token) => {
                await reader.ReadAsync(token).ConfigureAwait(false);
                long total = reader.GetInt64(0);
                await reader.NextResultAsync(token).ConfigureAwait(false);
                var items = new List<OutgoingEmailJournalEntry>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    items.Add(new OutgoingEmailJournalEntry(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        JsonSerializer.Deserialize<string[]>(reader.GetString(4)) ?? [], reader.GetString(5),
                        MailRelayQueueRowMapper.GetDateTimeOffset(reader, 6), await reader.IsDBNullAsync(7, token).ConfigureAwait(false) ? null : MailRelayQueueRowMapper.GetDateTimeOffset(reader, 7),
                        reader.GetInt32(8), reader.GetInt32(9), await reader.IsDBNullAsync(10, token).ConfigureAwait(false) ? null : reader.GetString(10),
                        await reader.IsDBNullAsync(11, token).ConfigureAwait(false) ? null : reader.GetString(11), !string.Equals(reader.GetString(2), "bug_report_received", StringComparison.Ordinal),
                        await reader.IsDBNullAsync(12, token).ConfigureAwait(false) ? null : reader.GetString(12), await reader.IsDBNullAsync(13, token).ConfigureAwait(false) ? null : reader.GetString(13)));
                }
                await reader.NextResultAsync(token).ConfigureAwait(false);
                var counts = new Dictionary<string, long>(StringComparer.Ordinal);
                while (await reader.ReadAsync(token).ConfigureAwait(false)) { counts.Add(reader.GetString(0), reader.GetInt64(1)); }
                return new OutgoingEmailJournalPage(items, total, counts);
            }, cancellationToken).ConfigureAwait(false);
    }
}
