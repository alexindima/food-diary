using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;

public sealed record ReduceActiveFastingTargetCommand(Guid? UserId, int ReducedHours)
    : ICommand<Result<FastingSessionModel>>, IUserRequest;
