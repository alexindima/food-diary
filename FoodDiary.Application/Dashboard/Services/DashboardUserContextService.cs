using FoodDiary.Results;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class DashboardUserContextService(IUserContextService userContextService) : IDashboardUserContextService {
    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userContextService.EnsureCanAccessAsync(userId, cancellationToken);

    public async Task<Result<DashboardUserContextModel>> GetAccessibleDashboardUserAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<DashboardUserContextModel>(userResult.Error)
            : Result.Success(ToDashboardUserContextModel(userResult.Value));
    }

    private static DashboardUserContextModel ToDashboardUserContextModel(User user) =>
        new(
            user.Id.Value,
            user.Email,
            user.Language,
            user.DashboardLayoutJson,
            user.DesiredWeight,
            user.DesiredWaist,
            user.HydrationGoal,
            user.WaterGoal,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            new UserCalorieSchedule(
                user.DailyCalorieTarget,
                user.CalorieCyclingEnabled,
                user.MondayCalories,
                user.TuesdayCalories,
                user.WednesdayCalories,
                user.ThursdayCalories,
                user.FridayCalories,
                user.SaturdayCalories,
                user.SundayCalories));
}
