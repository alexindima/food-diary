namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminMailInboxMessageSummaryHttpResponse(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Category,
    string Status,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset ReceivedAtUtc,
    string? EnvelopeFromAddress = null,
    bool IsTrustedRelay = false);
