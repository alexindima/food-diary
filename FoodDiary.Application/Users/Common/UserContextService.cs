using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

internal sealed class UserContextService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository) : IUserContextService, IUserProfileReadService, ICurrentUserAccessService {
    public async Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return accessError is not null
            ? Result.Failure<User>(accessError)
            : Result.Success(user!);
    }

    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure ? userResult.Error : null;
    }

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
        userWriteRepository.UpdateAsync(user, cancellationToken);

    public async Task<Result<UserModel>> GetUserAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserModel>(userResult.Error)
            : Result.Success(userResult.Value.ToModel());
    }

    public async Task<Result<GoalsModel>> GetGoalsAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<GoalsModel>(userResult.Error)
            : Result.Success(userResult.Value.ToGoalsModel());
    }

    public async Task<Result<UserDesiredWeightModel>> GetDesiredWeightAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserDesiredWeightModel>(userResult.Error)
            : Result.Success(ToDesiredWeightModel(userResult.Value));
    }

    private static UserDesiredWeightModel ToDesiredWeightModel(User user) {
        FoodDiary.Domain.Entities.Tracking.WeightGoal? goal = user.WeightGoals.SingleOrDefault(
            item => item.Status == FoodDiary.Domain.Enums.WeightGoalStatus.Active);
        return new UserDesiredWeightModel(user.DesiredWeight, goal?.StartWeight, goal?.StartedAtUtc);
    }

    public async Task<Result<IReadOnlyList<WeightGoalHistoryModel>>> GetWeightGoalHistoryAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WeightGoalHistoryModel>>(userResult.Error);
        }

        WeightGoalHistoryModel[] history = [.. userResult.Value.WeightGoals
            .OrderByDescending(static goal => goal.StartedAtUtc)
            .Select(static goal => new WeightGoalHistoryModel(
                goal.Id.Value,
                goal.TargetWeight,
                goal.StartWeight,
                goal.EndWeight,
                goal.StartedAtUtc,
                goal.EndedAtUtc,
                goal.Status.ToString()))];
        return Result.Success<IReadOnlyList<WeightGoalHistoryModel>>(history);
    }

    public async Task<Result<UserDesiredWaistModel>> GetDesiredWaistAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserDesiredWaistModel>(userResult.Error)
            : Result.Success(new UserDesiredWaistModel(userResult.Value.DesiredWaist));
    }

    public async Task<Result<UserNotificationPreferencesModel>> GetNotificationPreferencesAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserNotificationPreferencesModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new UserNotificationPreferencesModel(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours));
    }
}
