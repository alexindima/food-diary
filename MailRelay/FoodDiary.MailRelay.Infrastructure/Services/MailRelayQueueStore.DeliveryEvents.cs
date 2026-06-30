using NpgsqlTypes;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore {
    public async Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken) {
        string normalizedEmail = MailRelayQueueRowMapper.NormalizeEmail(request.Email)
                              ?? throw new InvalidOperationException("Delivery event email must be provided.");
        DateTimeOffset occurredAtUtc = request.OccurredAtUtc ?? timeProvider.GetUtcNow();
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

        DateTimeOffset createdAtUtc = await _executor.ScalarAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("eventType", request.EventType);
                command.Parameters.AddWithValue("email", normalizedEmail);
                command.Parameters.AddWithValue("source", request.Source);
                command.Parameters.AddWithValue("classification", (object?)request.Classification ?? DBNull.Value);
                command.Parameters.AddWithValue("providerMessageId", (object?)request.ProviderMessageId ?? DBNull.Value);
                command.Parameters.AddWithValue("reason", (object?)request.Reason ?? DBNull.Value);
                command.Parameters.AddWithValue("occurredAtUtc", occurredAtUtc);
            },
            MailRelayQueueRowMapper.ToDateTimeOffset,
            "Delivery event insert did not return created_at_utc.",
            cancellationToken).ConfigureAwait(false);

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

        return await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.Add("email", NpgsqlDbType.Text).Value = (object?)MailRelayQueueRowMapper.NormalizeEmail(email) ?? DBNull.Value;
            },
            async (reader, token) => {
                var result = new List<MailRelayDeliveryEventEntry>();
                while (await reader.ReadAsync(token).ConfigureAwait(false)) {
                    result.Add(await MailRelayQueueRowMapper.ReadDeliveryEventEntryAsync(reader, token).ConfigureAwait(false));
                }

                return result;
            },
            cancellationToken).ConfigureAwait(false);
    }

}
