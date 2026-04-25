namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessageSummaryHttpResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Status,
    DateTimeOffset ReceivedAtUtc);
