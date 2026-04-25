namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailMessageDetails(
    Guid Id,
    string? MessageId,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    string RawMime,
    string Status,
    DateTimeOffset ReceivedAtUtc);
