using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleFactorModel(
    Guid Id,
    Guid CycleProfileId,
    CycleFactorType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);
