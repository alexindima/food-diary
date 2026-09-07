namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesComplainedRecipientHttpRequest(
    string EmailAddress);
