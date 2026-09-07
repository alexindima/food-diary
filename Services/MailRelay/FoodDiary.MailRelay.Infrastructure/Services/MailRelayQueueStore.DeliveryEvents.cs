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
                                   provider_event_id,
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
                                   @providerEventId,
                                   @reason,
                                   @occurredAtUtc,
                                   now()
                           )
                           on conflict (source, provider_event_id, email, event_type) where provider_event_id is not null
                           do update set provider_event_id = excluded.provider_event_id
                           returning id, created_at_utc;
                           """;

        (Guid EventId, DateTimeOffset CreatedAtUtc) storedEvent = await _executor.QueryAsync(
            sql,
            command => {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("eventType", request.EventType);
                command.Parameters.AddWithValue("email", normalizedEmail);
                command.Parameters.AddWithValue("source", request.Source);
                command.Parameters.AddWithValue("classification", (object?)request.Classification ?? DBNull.Value);
                command.Parameters.AddWithValue("providerMessageId", (object?)request.ProviderMessageId ?? DBNull.Value);
                command.Parameters.AddWithValue("providerEventId", (object?)request.ProviderEventId ?? DBNull.Value);
                command.Parameters.AddWithValue("reason", (object?)request.Reason ?? DBNull.Value);
                command.Parameters.AddWithValue("occurredAtUtc", occurredAtUtc);
            },
            async (reader, token) => {
                await RequireReturnedRowAsync(reader, token, "Delivery event insert did not return an event.").ConfigureAwait(false);
                return (reader.GetGuid(0), MailRelayQueueRowMapper.GetDateTimeOffset(reader, 1));
            },
            cancellationToken).ConfigureAwait(false);

        return new MailRelayDeliveryEventEntry(
            storedEvent.EventId,
            request.EventType,
            normalizedEmail,
            request.Source,
            request.Classification,
            request.ProviderMessageId,
            request.Reason,
            occurredAtUtc,
            storedEvent.CreatedAtUtc,
            request.ProviderEventId);
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
                               provider_event_id,
                               reason,
                               occurred_at_utc,
                               created_at_utc
                           from mailrelay_delivery_events
                           where @email is null or email = @email
                           order by created_at_utc desc, id desc;
                           """;

        return await _executor.QueryAsync(
            sql,
            command => command.Parameters.Add("email", NpgsqlDbType.Text).Value = (object?)MailRelayQueueRowMapper.NormalizeEmail(email) ?? DBNull.Value,
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
