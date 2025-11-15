using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public record UpdateDesiredWeightCommand(
    UserId? UserId,
    double? DesiredWeight
) : ICommand<Result<UserDesiredWeightResponse>>;
