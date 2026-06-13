using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunWebhookHttpRequest(
    [property: JsonPropertyName("event-data")] MailgunEventDataHttpRequest EventData,
    MailgunSignatureHttpRequest? Signature = null);
