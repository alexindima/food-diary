using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.ReduceActiveFastingTarget;

public sealed record ReduceActiveFastingTargetCommand(Guid? UserId, int ReducedHours)
    : ICommand<Result<FastingSessionModel>>, IUserRequest;
