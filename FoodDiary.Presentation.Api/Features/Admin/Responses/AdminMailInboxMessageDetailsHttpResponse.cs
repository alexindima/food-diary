namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessageDetailsHttpResponse(
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
