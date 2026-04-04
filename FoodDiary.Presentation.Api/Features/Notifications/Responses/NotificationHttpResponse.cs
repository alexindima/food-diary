namespace FoodDiary.Presentation.Api.Features.Notifications.Responses;

public sealed record NotificationHttpResponse(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? ReferenceId,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record UnreadCountHttpResponse(int Count);
