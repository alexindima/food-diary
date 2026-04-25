namespace FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

public sealed record InboundMailMessageSummaryHttpResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Status,
    DateTimeOffset ReceivedAtUtc);
