using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record BleedingEntryReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    BleedingType Type,
    CycleFlowLevel Flow,
    int? PainImpact,
    string? Notes);
