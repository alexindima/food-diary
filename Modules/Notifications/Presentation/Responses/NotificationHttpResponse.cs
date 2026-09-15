namespace FoodDiary.Modules.Notifications.Presentation.Responses;

public sealed record NotificationHttpResponse(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? TargetUrl,
    string? ReferenceId,
    bool IsRead,
    DateTime CreatedAtUtc);
