using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateUserAppearance;

public sealed record UpdateUserAppearanceCommand(
    Guid? UserId,
    string? Theme,
    string? UiStyle,
    string? SurfaceStyle = null
) : ICommand<Result<UserModel>>, IUserRequest;
