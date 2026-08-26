namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunMessageHttpRequest(
    MailgunMessageHeadersHttpRequest? Headers = null);
