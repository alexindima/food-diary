using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record MenstrualEpisodeModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime StartDate,
    DateTime? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
