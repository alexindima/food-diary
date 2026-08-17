using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record MenstrualEpisodeModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly StartDate,
    DateOnly? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
