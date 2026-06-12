namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record ReceiveInboundMailRequest(
    string? MessageId,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    string RawMime,
    DateTimeOffset ReceivedAtUtc);
