using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
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
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UserModel>(userIdResult);
        }

        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserModel>(userResult.Error);
        }

        Result<UserAppearancePreferences> preferencesResult = UserAppearancePreferencesParser.ParseOptional(
            command.Theme,
            command.UiStyle);
        if (preferencesResult.IsFailure) {
            return Result.Failure<UserModel>(preferencesResult.Error);
        }

        User user = userResult.Value;
        UserAppearancePreferences preferences = preferencesResult.Value;
        user.UpdatePreferences(new UserPreferenceUpdate(
            Theme: preferences.Theme,
            UiStyle: preferences.UiStyle));

        await userContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        return Result.Success(user.ToModel());
    }
}
