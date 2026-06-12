namespace FoodDiary.MailInbox.Client.Models;

public sealed record InboundMailMessageDetailsResponse(
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
