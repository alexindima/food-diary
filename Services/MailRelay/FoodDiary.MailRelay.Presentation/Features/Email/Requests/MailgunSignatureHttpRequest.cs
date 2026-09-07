namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunSignatureHttpRequest(
    string Timestamp,
    string Token,
    string Signature);
