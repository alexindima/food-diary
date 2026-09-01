using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public record UpdateDesiredWaistCommand(
    Guid? UserId,
    double? DesiredWaistCm
) : ICommand<Result<UserDesiredWaistModel>>, IUserRequest;
