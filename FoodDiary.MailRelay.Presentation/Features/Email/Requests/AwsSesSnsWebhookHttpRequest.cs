namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesSnsWebhookHttpRequest(
    string Type,
    string? Message,
    string? SubscribeURL = null);

public sealed record AwsSesNotificationHttpModel(
    string NotificationType,
    AwsSesMailHttpModel Mail,
    AwsSesBounceHttpModel? Bounce = null,
    AwsSesComplaintHttpModel? Complaint = null);

public sealed record AwsSesMailHttpModel(
    string? MessageId,
    IReadOnlyList<string> Destination);

public sealed record AwsSesBounceHttpModel(
    string? BounceType,
    IReadOnlyList<AwsSesBouncedRecipientHttpModel> BouncedRecipients);

public sealed record AwsSesBouncedRecipientHttpModel(
    string EmailAddress,
    string? DiagnosticCode = null);

public sealed record AwsSesComplaintHttpModel(
    IReadOnlyList<AwsSesComplainedRecipientHttpModel> ComplainedRecipients);

public sealed record AwsSesComplainedRecipientHttpModel(
    string EmailAddress);
