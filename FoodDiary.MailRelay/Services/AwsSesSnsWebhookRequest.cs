namespace FoodDiary.MailRelay.Services;

public sealed record AwsSesSnsWebhookRequest(
    string Type,
    string? Message,
    string? SubscribeURL = null);

public sealed record AwsSesNotificationMessage(
    string NotificationType,
    AwsSesMailInfo Mail,
    AwsSesBounceInfo? Bounce = null,
    AwsSesComplaintInfo? Complaint = null);

public sealed record AwsSesMailInfo(
    string? MessageId,
    IReadOnlyList<string> Destination);

public sealed record AwsSesBounceInfo(
    string? BounceType,
    IReadOnlyList<AwsSesBouncedRecipient> BouncedRecipients);

public sealed record AwsSesBouncedRecipient(
    string EmailAddress,
    string? DiagnosticCode = null);

public sealed record AwsSesComplaintInfo(
    IReadOnlyList<AwsSesComplainedRecipient> ComplainedRecipients);

public sealed record AwsSesComplainedRecipient(
    string EmailAddress);
