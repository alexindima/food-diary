using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleFactorModel(
    Guid Id,
    Guid CycleProfileId,
    CycleFactorType Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes);
