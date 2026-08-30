using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Dietologist.Models;

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
