using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Services;

public sealed record MailgunWebhookRequest(
    [property: JsonPropertyName("event-data")] MailgunEventData EventData);

public sealed record MailgunEventData(
    string Event,
    string Recipient,
    string? Id = null,
    string? Severity = null,
    string? Reason = null);
