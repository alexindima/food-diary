using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;

using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateUserAppearance;

public sealed class UpdateUserAppearanceCommandHandler(IUserContextService userContextService)
    : ICommandHandler<UpdateUserAppearanceCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(UpdateUserAppearanceCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserModel>(userIdResult);
        }

        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserModel>(userResult.Error);
        }

        Result<UserAppearancePreferences> preferencesResult = UserAppearancePreferencesParser.ParseOptional(
            command.Theme,
            command.UiStyle,
            command.SurfaceStyle);
        if (preferencesResult.IsFailure) {
            return Result.Failure<UserModel>(preferencesResult.Error);
        }

        User user = userResult.Value;
        UserAppearancePreferences preferences = preferencesResult.Value;
        user.UpdatePreferences(new UserPreferenceUpdate(
            Theme: preferences.Theme,
            UiStyle: preferences.UiStyle,
            SurfaceStyle: preferences.SurfaceStyle));

        await userContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        return Result.Success(user.ToModel());
    }
}
