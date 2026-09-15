using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Services;

internal sealed class UserNotificationProfileService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository) : IUserNotificationProfileService {
    public async Task<Result<UserNotificationProfileModel>> GetAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserNotificationProfileModel>(userResult.Error)
            : Result.Success(ToModel(userResult.Value));
    }

    public async Task<Result<UserNotificationProfileModel>> UpdatePreferencesAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserNotificationProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        user.UpdatePreferences(update);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToModel(user));
    }

    private async Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return error is null ? Result.Success(user!) : Result.Failure<User>(error);
    }

    private static UserNotificationProfileModel ToModel(User user) =>
        new(
            user.Id,
            user.HasPassword,
            user.Language,
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);
}
