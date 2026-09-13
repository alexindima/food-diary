namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminMailInboxMessageSummaryModel(
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
