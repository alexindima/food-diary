namespace FoodDiary.Modules.Notifications.Application.Models;

public sealed record NotificationModel(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? TargetUrl,
    string? ReferenceId,
    bool IsRead,
    DateTime CreatedAtUtc);
