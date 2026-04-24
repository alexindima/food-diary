using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Commands.UpdateUserAppearance;

public sealed class UpdateUserAppearanceCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateUserAppearanceCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(UpdateUserAppearanceCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(new UserId(command.UserId.Value), cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<UserModel>(accessError);
        }

        var themeResult = StringCodeParser.ParseOptionalTheme(
            command.Theme,
            nameof(UpdateUserAppearanceCommand.Theme),
            "Invalid theme value.");
        if (themeResult.IsFailure) {
            return Result.Failure<UserModel>(themeResult.Error);
        }

        var uiStyleResult = StringCodeParser.ParseOptionalUiStyle(
            command.UiStyle,
            nameof(UpdateUserAppearanceCommand.UiStyle),
            "Invalid UI style value.");
        if (uiStyleResult.IsFailure) {
            return Result.Failure<UserModel>(uiStyleResult.Error);
        }

        user!.UpdatePreferences(new UserPreferenceUpdate(
            Theme: themeResult.Value,
            UiStyle: uiStyleResult.Value));

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(user.ToModel());
    }
}
