using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record CycleFactorReadModel(
    Guid Id,
    Guid CycleProfileId,
    CycleFactorType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);
