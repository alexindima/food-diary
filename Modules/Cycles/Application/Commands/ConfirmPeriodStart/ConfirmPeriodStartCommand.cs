using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.ConfirmPeriodStart;

public sealed record ConfirmPeriodStartCommand(Guid? UserId, Guid CycleProfileId, DateOnly Date)
    : ICommand<Result<CycleModel>>, IUserRequest;
