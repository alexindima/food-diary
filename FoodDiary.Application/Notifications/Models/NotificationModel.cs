namespace FoodDiary.Application.Notifications.Models;

public sealed record NotificationModel(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? ReferenceId,
    bool IsRead,
    DateTime CreatedAtUtc);
