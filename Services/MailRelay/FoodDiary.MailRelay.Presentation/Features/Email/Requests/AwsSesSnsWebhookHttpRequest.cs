using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesSnsWebhookHttpRequest(
    string Type,
    string? Message,
    string? MessageId = null,
    string? TopicArn = null,
    string? Subject = null,
    string? Timestamp = null,
    string? SignatureVersion = null,
    string? Signature = null,
    [property: JsonPropertyName("SigningCertURL")] string? SigningCertUrl = null,
    [property: JsonPropertyName("SubscribeURL")] string? SubscribeUrl = null,
    string? Token = null,
    [property: JsonPropertyName("UnsubscribeURL")] string? UnsubscribeUrl = null);
