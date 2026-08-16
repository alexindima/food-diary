using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record MenstrualEpisodeReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateTime StartDate,
    DateTime? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
