using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Commands.UpdateUserAppearance;

public sealed record UpdateUserAppearanceCommand(
    Guid? UserId,
    string? Theme,
    string? UiStyle
) : ICommand<Result<UserModel>>, IUserRequest;
