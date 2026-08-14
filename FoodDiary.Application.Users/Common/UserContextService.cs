using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

internal sealed class UserContextService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository) :
    IUserContextService,
    IUserProfileReadService,
    ICurrentUserAccessService,
    IUserAiProfileReadService,
    IUserDashboardProfileReadService,
    IUserDietologistProfileReadService,
    IUserGamificationProfileReadService,
    IUserHydrationProfileReadService,
    IUserTdeeProfileReadService,
    IUserWeeklyCheckInProfileReadService {
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

    public async Task<Result<UserAiProfileModel>> GetAiProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserAiProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new UserAiProfileModel(
            user.Id,
            user.Language,
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit));
    }

    public async Task<Result<UserDashboardProfileModel>> GetDashboardProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserDashboardProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new UserDashboardProfileModel(
            user.Id.Value,
            user.Email,
            user.Language,
            user.DashboardLayoutJson,
            user.DesiredWeightKg,
            user.DesiredWaistCm,
            user.HydrationGoal,
            user.WaterGoal,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            CreateCalorieSchedule(user)));
    }

    public async Task<Result<UserGamificationProfileModel>> GetGamificationProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserGamificationProfileModel>(userResult.Error)
            : Result.Success(new UserGamificationProfileModel(CreateCalorieSchedule(userResult.Value)));
    }

    public async Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserDietologistProfileModel>(userResult.Error)
            : Result.Success(ToDietologistProfile(userResult.Value));
    }

    public async Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is null ? null : ToDietologistProfile(user);
    }

    public async Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) {
        User? user = await userLookupRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        return user is null ? null : ToDietologistProfile(user);
    }

    public async Task<Result<UserHydrationProfileModel>> GetHydrationProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserHydrationProfileModel>(userResult.Error)
            : Result.Success(new UserHydrationProfileModel(userResult.Value.HydrationGoal ?? userResult.Value.WaterGoal));
    }

    public async Task<Result<UserTdeeProfileModel>> GetTdeeProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserTdeeProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new UserTdeeProfileModel(
            user.CalculateBmr(),
            user.CalculateEstimatedTdee(),
            user.WeightKg,
            user.DesiredWeightKg,
            user.DailyCalorieTarget));
    }

    public async Task<Result<UserWeeklyCheckInProfileModel>> GetWeeklyCheckInProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserWeeklyCheckInProfileModel>(userResult.Error)
            : Result.Success(new UserWeeklyCheckInProfileModel(userResult.Value.DailyCalorieTarget));
    }

    private static FoodDiary.Domain.ValueObjects.UserCalorieSchedule CreateCalorieSchedule(User user) =>
        new(
            user.DailyCalorieTarget,
            user.CalorieCyclingEnabled,
            user.MondayCalories,
            user.TuesdayCalories,
            user.WednesdayCalories,
            user.ThursdayCalories,
            user.FridayCalories,
            user.SaturdayCalories,
            user.SundayCalories);

    private static UserDietologistProfileModel ToDietologistProfile(User user) =>
        new(
            user.Id.Value,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Language,
            user.HasRole(RoleNames.Dietologist));

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
        return new UserDesiredWeightModel(user.DesiredWeightKg, goal?.StartWeightKg, goal?.StartedAtUtc);
    }

    public async Task<Result<IReadOnlyList<WeightGoalHistoryModel>>> GetWeightGoalHistoryAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WeightGoalHistoryModel>>(userResult.Error);
        }

        return Result.Success(ToWeightGoalHistory(userResult.Value));
    }

    public async Task<Result<UserDesiredWaistModel>> GetDesiredWaistAsync(UserId userId, CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<UserDesiredWaistModel>(userResult.Error)
            : Result.Success(ToDesiredWaistModel(userResult.Value));
    }

    private static UserDesiredWaistModel ToDesiredWaistModel(User user) {
        FoodDiary.Domain.Entities.Tracking.WaistGoal? goal = user.WaistGoals.SingleOrDefault(
            item => item.Status == FoodDiary.Domain.Enums.WaistGoalStatus.Active);
        return new UserDesiredWaistModel(user.DesiredWaistCm, goal?.StartWaistCm, goal?.StartedAtUtc);
    }

    public async Task<Result<IReadOnlyList<WaistGoalHistoryModel>>> GetWaistGoalHistoryAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WaistGoalHistoryModel>>(userResult.Error);
        }

        return Result.Success(ToWaistGoalHistory(userResult.Value));
    }

    public async Task<Result<WeightHistoryProfileModel>> GetWeightHistoryProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<WeightHistoryProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new WeightHistoryProfileModel(
            user.HeightCm,
            ToDesiredWeightModel(user),
            ToWeightGoalHistory(user)));
    }

    public async Task<Result<WaistHistoryProfileModel>> GetWaistHistoryProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<WaistHistoryProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new WaistHistoryProfileModel(
            user.HeightCm,
            ToDesiredWaistModel(user),
            ToWaistGoalHistory(user)));
    }

    private static IReadOnlyList<WeightGoalHistoryModel> ToWeightGoalHistory(User user) =>
        [.. user.WeightGoals
            .OrderByDescending(static goal => goal.StartedAtUtc)
            .Select(static goal => new WeightGoalHistoryModel(
                goal.Id.Value,
                goal.TargetWeightKg,
                goal.StartWeightKg,
                goal.EndWeightKg,
                goal.StartedAtUtc,
                goal.EndedAtUtc,
                goal.Status.ToString()))];

    private static IReadOnlyList<WaistGoalHistoryModel> ToWaistGoalHistory(User user) =>
        [.. user.WaistGoals
            .OrderByDescending(static goal => goal.StartedAtUtc)
            .Select(static goal => new WaistGoalHistoryModel(
                goal.Id.Value,
                goal.TargetWaistCm,
                goal.StartWaistCm,
                goal.EndWaistCm,
                goal.StartedAtUtc,
                goal.EndedAtUtc,
                goal.Status.ToString()))];

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
