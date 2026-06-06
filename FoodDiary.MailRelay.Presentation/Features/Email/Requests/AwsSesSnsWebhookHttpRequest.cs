namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesSnsWebhookHttpRequest(
    string Type,
    string? Message,
    string? SubscribeURL = null);
