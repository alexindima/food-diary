using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public record UpdateDesiredWaistCommand(
    Guid? UserId,
    double? DesiredWaist
) : ICommand<Result<UserDesiredWaistModel>>, IUserRequest;
