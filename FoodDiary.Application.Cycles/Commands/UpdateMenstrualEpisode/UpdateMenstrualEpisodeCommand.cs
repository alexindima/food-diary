using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.UpdateMenstrualEpisode;

public sealed record UpdateMenstrualEpisodeCommand(
    Guid? UserId,
    Guid CycleProfileId,
    Guid MenstrualEpisodeId,
    DateTime StartDate,
    DateTime? EndDate)
    : ICommand<Result<CycleModel>>;
