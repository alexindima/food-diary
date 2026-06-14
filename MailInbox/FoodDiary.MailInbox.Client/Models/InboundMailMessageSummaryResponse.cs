namespace FoodDiary.MailInbox.Client.Models;

public sealed record InboundMailMessageSummaryResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Category,
    string Status,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset ReceivedAtUtc);
