namespace FoodDiary.MailRelay.Application.Emails.Models;

public sealed record RelayEmailMessageRequest(
    string FromAddress,
    string FromName,
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
