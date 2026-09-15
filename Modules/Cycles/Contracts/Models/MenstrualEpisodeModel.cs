using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record MenstrualEpisodeModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly StartDate,
    DateOnly? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
