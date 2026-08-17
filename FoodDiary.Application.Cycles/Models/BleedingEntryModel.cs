using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record BleedingEntryModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    BleedingType Type,
    CycleFlowLevel Flow,
    int? PainImpact,
    string? Notes);
