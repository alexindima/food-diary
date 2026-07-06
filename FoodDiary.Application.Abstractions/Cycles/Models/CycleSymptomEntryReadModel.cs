using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CycleSymptomEntryReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    CycleSymptomCategory Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
