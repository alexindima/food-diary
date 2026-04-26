namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminMailInboxMessageDetailsModel(
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
