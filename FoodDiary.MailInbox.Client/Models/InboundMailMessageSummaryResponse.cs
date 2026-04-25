namespace FoodDiary.MailInbox.Client.Models;

public sealed record InboundMailMessageSummaryResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Status,
    DateTimeOffset ReceivedAtUtc);
