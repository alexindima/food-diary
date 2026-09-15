using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWeight;

public record UpdateDesiredWeightCommand(
    Guid? UserId,
    double? DesiredWeightKg
) : ICommand<Result<UserDesiredWeightModel>>, IUserRequest;
