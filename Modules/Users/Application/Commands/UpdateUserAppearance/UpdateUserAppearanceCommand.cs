using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateUserAppearance;

public sealed record UpdateUserAppearanceCommand(
    Guid? UserId,
    string? Theme,
    string? UiStyle
) : ICommand<Result<UserModel>>, IUserRequest;
