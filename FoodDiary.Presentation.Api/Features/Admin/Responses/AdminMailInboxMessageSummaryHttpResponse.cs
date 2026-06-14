namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessageSummaryHttpResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Category,
    string Status,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset ReceivedAtUtc);
