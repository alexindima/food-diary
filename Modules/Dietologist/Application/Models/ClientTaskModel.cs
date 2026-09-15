using FoodDiary.Modules.Dietologist.Domain.Enums;

namespace FoodDiary.Modules.Dietologist.Application.Models;

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
