namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesComplaintHttpRequest(
    IReadOnlyList<AwsSesComplainedRecipientHttpRequest> ComplainedRecipients);
