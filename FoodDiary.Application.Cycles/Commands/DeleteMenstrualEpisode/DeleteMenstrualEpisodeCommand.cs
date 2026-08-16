using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.DeleteMenstrualEpisode;

public sealed record DeleteMenstrualEpisodeCommand(
    Guid? UserId,
    Guid CycleProfileId,
    Guid MenstrualEpisodeId)
    : ICommand<Result<CycleModel>>;
