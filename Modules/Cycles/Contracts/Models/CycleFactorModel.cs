using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record CycleFactorModel(
    Guid Id,
    Guid CycleProfileId,
    CycleFactorType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);
