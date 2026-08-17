using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record MenstrualEpisodeReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly StartDate,
    DateOnly? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
