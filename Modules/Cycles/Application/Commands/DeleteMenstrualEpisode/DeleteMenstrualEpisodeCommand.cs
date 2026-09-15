using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.DeleteMenstrualEpisode;

public sealed record DeleteMenstrualEpisodeCommand(
    Guid? UserId,
    Guid CycleProfileId,
    Guid MenstrualEpisodeId)
    : ICommand<Result<CycleModel>>;
