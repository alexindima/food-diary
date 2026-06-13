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
    string? SigningCertURL = null,
    string? SubscribeURL = null,
    string? Token = null,
    string? UnsubscribeURL = null);
