namespace FoodDiary.Application.Admin.Models;

public sealed record AdminMailInboxMessageSummaryModel(
    Guid Id,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string Status,
    DateTimeOffset ReceivedAtUtc);
