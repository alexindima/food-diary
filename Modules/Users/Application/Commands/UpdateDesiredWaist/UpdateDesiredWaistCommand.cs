using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWaist;

public record UpdateDesiredWaistCommand(
    Guid? UserId,
    double? DesiredWaistCm
) : ICommand<Result<UserDesiredWaistModel>>, IUserRequest;
