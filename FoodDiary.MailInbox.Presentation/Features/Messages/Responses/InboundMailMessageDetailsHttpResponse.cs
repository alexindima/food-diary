namespace FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

public sealed record InboundMailMessageDetailsHttpResponse(
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
