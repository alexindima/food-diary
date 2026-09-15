using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record CycleSymptomEntryReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    CycleSymptomCategory Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
