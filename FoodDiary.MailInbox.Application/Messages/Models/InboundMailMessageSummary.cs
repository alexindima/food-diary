namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailMessageSummary(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Status,
    DateTimeOffset ReceivedAtUtc);
