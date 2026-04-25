using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunWebhookHttpRequest(
    [property: JsonPropertyName("event-data")] MailgunEventDataHttpModel EventData);

public sealed record MailgunEventDataHttpModel(
    string Event,
    string Recipient,
    string? Id = null,
    string? Severity = null,
    string? Reason = null);
