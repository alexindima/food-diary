using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record CycleSymptomEntryModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    CycleSymptomCategory Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
