using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Abstractions.Models;

public sealed record MenstrualEpisodeReadModel(
    Guid Id,
    Guid CycleProfileId,
    DateOnly StartDate,
    DateOnly? EndDate,
    MenstrualEpisodeStatus Status,
    bool ExcludedFromPredictions);
