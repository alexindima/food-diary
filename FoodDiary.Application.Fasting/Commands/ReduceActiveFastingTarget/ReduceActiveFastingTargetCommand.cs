using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;

public sealed record ReduceActiveFastingTargetCommand(Guid? UserId, int ReducedHours)
    : ICommand<Result<FastingSessionModel>>, IUserRequest;
