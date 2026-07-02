using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateUserAppearance;

public sealed class UpdateUserAppearanceCommandHandler(IUserContextService userContextService)
    : ICommandHandler<UpdateUserAppearanceCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(UpdateUserAppearanceCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        Result<User> userResult = await userContextService.GetAccessibleUserAsync(new UserId(command.UserId.Value), cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserModel>(userResult.Error);
        }

        Result<string?> themeResult = StringCodeParser.ParseOptionalTheme(
            command.Theme,
            nameof(UpdateUserAppearanceCommand.Theme),
            "Invalid theme value.");
        if (themeResult.IsFailure) {
            return Result.Failure<UserModel>(themeResult.Error);
        }

        Result<string?> uiStyleResult = StringCodeParser.ParseOptionalUiStyle(
            command.UiStyle,
            nameof(UpdateUserAppearanceCommand.UiStyle),
            "Invalid UI style value.");
        if (uiStyleResult.IsFailure) {
            return Result.Failure<UserModel>(uiStyleResult.Error);
        }

        User user = userResult.Value;
        user.UpdatePreferences(new UserPreferenceUpdate(
            Theme: themeResult.Value,
            UiStyle: uiStyleResult.Value));

        await userContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        return Result.Success(user.ToModel());
    }
}
