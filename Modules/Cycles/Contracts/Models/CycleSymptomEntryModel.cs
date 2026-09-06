using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleSymptomEntryModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly Date,
    CycleSymptomCategory Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
