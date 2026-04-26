using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public record UpdateDesiredWaistCommand(
    Guid? UserId,
    double? DesiredWaist
) : ICommand<Result<UserDesiredWaistModel>>, IUserRequest;
