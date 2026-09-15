using FoodDiary.Modules.Dietologist.Domain.Enums;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record ClientTaskReadModel(
    Guid Id,
    Guid DietologistUserId,
    Guid ClientUserId,
    string Title,
    string? Details,
    DateTime? DueAtUtc,
    ClientTaskStatus Status,
    DateTime CreatedAtUtc,
    DateTime? StatusChangedAtUtc);
