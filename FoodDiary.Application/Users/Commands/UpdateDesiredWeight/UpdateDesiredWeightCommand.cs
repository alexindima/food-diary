using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public record UpdateDesiredWeightCommand(
    Guid? UserId,
    double? DesiredWeight
) : ICommand<Result<UserDesiredWeightModel>>, IUserRequest;
