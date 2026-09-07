using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunMessageHeadersHttpRequest(
    [property: JsonPropertyName("message-id")] string? MessageId = null);
