namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record MailgunEventDataHttpRequest(
    string Event,
    string Recipient,
    string? Id = null,
    string? Severity = null,
    string? Reason = null);
