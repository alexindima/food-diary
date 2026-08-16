using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.ConfirmPeriodStart;

public sealed record ConfirmPeriodStartCommand(Guid? UserId, Guid CycleProfileId, DateTime Date)
    : ICommand<Result<CycleModel>>, IUserRequest;
