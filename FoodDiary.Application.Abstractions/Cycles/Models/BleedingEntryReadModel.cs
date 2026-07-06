using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record BleedingEntryReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    BleedingType Type,
    CycleFlowLevel Flow,
    int? PainImpact,
    string? Notes);
