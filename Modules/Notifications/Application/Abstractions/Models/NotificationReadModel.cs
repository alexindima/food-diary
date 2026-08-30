namespace FoodDiary.Application.Abstractions.Notifications.Models;

public sealed record NotificationReadModel(
    Guid Id,
    string Type,
    string? ReferenceId,
    string PayloadJson,
    bool IsRead,
    DateTime CreatedAtUtc);
