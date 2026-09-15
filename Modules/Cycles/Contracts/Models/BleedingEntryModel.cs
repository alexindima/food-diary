using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record BleedingEntryModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    BleedingType Type,
    CycleFlowLevel Flow,
    int? PainImpact,
    string? Notes);
