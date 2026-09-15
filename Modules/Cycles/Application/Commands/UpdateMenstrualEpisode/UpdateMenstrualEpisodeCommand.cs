using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpdateMenstrualEpisode;

public sealed record UpdateMenstrualEpisodeCommand(
    Guid? UserId,
    Guid CycleProfileId,
    Guid MenstrualEpisodeId,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool? ExcludedFromPredictions = null)
    : ICommand<Result<CycleModel>>;
