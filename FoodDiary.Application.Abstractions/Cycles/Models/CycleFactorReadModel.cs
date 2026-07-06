using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CycleFactorReadModel(
    Guid Id,
    Guid CycleProfileId,
    CycleFactorType Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes);
