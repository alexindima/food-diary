using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore {
    public async Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(
        string? email, CancellationToken cancellationToken) =>
        (await GetSuppressionsPageAsync(email, 1, 100, cancellationToken).ConfigureAwait(false)).Data;

    public async Task<MailRelayPage<MailRelaySuppressionEntry>> GetSuppressionsPageAsync(
        string? email, int page, int limit, CancellationToken cancellationToken) {
        int normalizedPage = Math.Clamp(page, 1, 10_000);
        int normalizedLimit = Math.Clamp(limit, 1, 100);
        int offset = (normalizedPage - 1) * normalizedLimit;
        const string sql = """
                           select count(*)
                           from mailrelay_suppressions
                           where @email is null or email = @email;

                           select
                               email,
                               reason,
                               source,
                               created_at_utc,
                               updated_at_utc,
                               expires_at_utc
                           from mailrelay_suppressions
                           where @email is null or email = @email
                           order by updated_at_utc desc, email asc
                           limit @limit offset @offset;
                           """;

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.Add("email", NpgsqlDbType.Text).Value = (object?)MailRelayQueueRowMapper.NormalizeEmail(email) ?? DBNull.Value;
                command.Parameters.AddWithValue("limit", normalizedLimit);
                command.Parameters.AddWithValue("offset", offset);
            },
            async (reader, token) => {
                await RequireReturnedRowAsync(reader, token, "Suppression count query did not return a row.").ConfigureAwait(false);
                int total = checked((int)reader.GetInt64(0));
                await reader.NextResultAsync(token).ConfigureAwait(false);
                var entries = new List<MailRelaySuppressionEntry>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    entries.Add(await MailRelayQueueRowMapper.ReadSuppressionEntryAsync(reader, token).ConfigureAwait(false));
                }

                int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedLimit);
                return new MailRelayPage<MailRelaySuppressionEntry>(entries, normalizedPage, normalizedLimit, totalPages, total);
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) {
        string normalizedEmail = MailRelayQueueRowMapper.NormalizeEmail(request.Email)
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

        NpgsqlConnection connection = await DataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("email", normalizedEmail);
                command.Parameters.AddWithValue("reason", request.Reason);
                command.Parameters.AddWithValue("source", request.Source);
                command.Parameters.AddWithValue("expiresAtUtc", (object?)request.ExpiresAtUtc ?? DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) {
        const string sql = "delete from mailrelay_suppressions where email = @email;";

        string normalizedEmail = MailRelayQueueRowMapper.NormalizeEmail(email)
                              ?? throw new InvalidOperationException("Suppression email must be provided.");

        int deletedRows = await _executor.ExecuteAsync(
            sql,
            command => command.Parameters.AddWithValue("email", normalizedEmail),
            cancellationToken).ConfigureAwait(false);

        return deletedRows > 0;
    }

    public async Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken) {
        string?[] normalizedRecipients = [.. recipients
            .Select(MailRelayQueueRowMapper.NormalizeEmail)
            .Where(static email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.Ordinal)];

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

        return await _executor.QueryAsync(
            sql,
            command => command.Parameters.AddWithValue("emails", normalizedRecipients),
            async (reader, token) => {
                var emails = new List<string>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    emails.Add(reader.GetString(0));
                }

                return emails;
            },
            cancellationToken).ConfigureAwait(false);
    }

}
