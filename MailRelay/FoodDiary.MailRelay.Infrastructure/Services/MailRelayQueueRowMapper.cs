using System.Text.Json;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public static class MailRelayQueueRowMapper {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static QueuedEmailMessage ReadQueuedEmailMessage(NpgsqlDataReader reader) {
        string toRecipientsJson = reader.GetString(3);
        string[] recipients = JsonSerializer.Deserialize<string[]>(toRecipientsJson, JsonOptions)
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
            reader.GetInt32(9),
            reader.FieldCount > 10 && !reader.IsDBNull(10) ? GetDateTimeOffset(reader, 10) : null,
            reader.FieldCount > 11 && !reader.IsDBNull(11) ? GetDateTimeOffset(reader, 11) : null);
    }

    public static MailRelayOutboxMessage ReadOutboxMessage(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetInt32(2));

    public static async Task<MailRelaySuppressionEntry> ReadSuppressionEntryAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken) =>
        new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            GetDateTimeOffset(reader, 3),
            GetDateTimeOffset(reader, 4),
            await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : GetDateTimeOffset(reader, 5));

    public static async Task<MailRelayDeliveryEventEntry> ReadDeliveryEventEntryAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4),
            await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(5),
            await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
            GetDateTimeOffset(reader, 7),
            GetDateTimeOffset(reader, 8));

    public static MailRelayQueueStats ReadQueueStats(NpgsqlDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));

    public static async Task<MailRelayMessageDetails> ReadMessageDetailsAsync(
        NpgsqlDataReader reader,
        IReadOnlyList<string> suppressedRecipients,
        CancellationToken cancellationToken) =>
        new(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            GetDateTimeOffset(reader, 6),
            GetDateTimeOffset(reader, 7),
            await reader.IsDBNullAsync(8, cancellationToken).ConfigureAwait(false) ? null : GetDateTimeOffset(reader, 8),
            await reader.IsDBNullAsync(9, cancellationToken).ConfigureAwait(false) ? null : GetDateTimeOffset(reader, 9),
            await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(10),
            suppressedRecipients);

    public static string[] ReadMessageRecipients(NpgsqlDataReader reader) =>
        JsonSerializer.Deserialize<string[]>(reader.GetString(11), JsonOptions) ?? [];

    public static DateTimeOffset GetDateTimeOffset(NpgsqlDataReader reader, int ordinal) =>
        ToDateTimeOffset(reader.GetValue(ordinal));

    public static DateTimeOffset ToDateTimeOffset(object value) =>
        value switch {
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime(),
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException($"Unexpected timestamp value type: {value.GetType().FullName}."),
        };

    public static string? NormalizeEmail(string? email) {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
