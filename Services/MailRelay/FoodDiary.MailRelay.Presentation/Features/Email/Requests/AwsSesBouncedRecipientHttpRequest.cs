namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record AwsSesBouncedRecipientHttpRequest(
    string EmailAddress,
    string? DiagnosticCode = null);
