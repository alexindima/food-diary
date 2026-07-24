using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Dietologist.Models;

public sealed record ClientTaskModel(
    Guid Id,
    Guid DietologistUserId,
    Guid ClientUserId,
    string Title,
    string? Details,
    DateTime? DueAtUtc,
    ClientTaskStatus Status,
    bool IsOverdue,
    DateTime CreatedAtUtc,
    DateTime? StatusChangedAtUtc);
