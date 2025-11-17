using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public record UpdateDesiredWaistCommand(
    UserId? UserId,
    double? DesiredWaist
) : ICommand<Result<UserDesiredWaistResponse>>;
