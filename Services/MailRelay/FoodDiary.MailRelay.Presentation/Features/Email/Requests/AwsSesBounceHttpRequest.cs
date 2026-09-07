namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesBounceHttpRequest(
    string? BounceType,
    IReadOnlyList<AwsSesBouncedRecipientHttpRequest> BouncedRecipients);
