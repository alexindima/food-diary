namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesNotificationHttpRequest(
    string NotificationType,
    AwsSesMailHttpRequest Mail,
    AwsSesBounceHttpRequest? Bounce = null,
    AwsSesComplaintHttpRequest? Complaint = null);
