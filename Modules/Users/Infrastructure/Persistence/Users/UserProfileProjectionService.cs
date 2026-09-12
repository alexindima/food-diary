using System.Linq.Expressions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserProfileProjectionService(FoodDiaryDbContext context) :
    ICurrentUserAccessService, IUserAiProfileReadService, IUserDashboardProfileReadService,
    IUserDietologistProfileReadService, IUserGamificationProfileReadService,
    IUserHydrationProfileReadService, IUserTdeeProfileReadService, IUserWeeklyCheckInProfileReadService {
    private IQueryable<User> AccessibleUsers => context.Users.AsNoTracking()
        .Where(user => user.IsActive && user.DeletedAt == null);

    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await AccessibleUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken).ConfigureAwait(false)
            ? null : Errors.Authentication.InvalidToken;

    public Task<Result<UserAiProfileModel>> GetAiProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserAiProfileModel(user.Id, user.Language,
            user.AiInputTokenLimit, user.AiOutputTokenLimit, user.AiConsentAcceptedAt != null), cancellationToken);

    public Task<Result<UserDashboardProfileModel>> GetDashboardProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserDashboardProfileModel(user.Id.Value, user.Email, user.Language,
            user.DashboardLayoutJson, user.DesiredWeightKg, user.DesiredWaistCm, user.HydrationGoal, user.WaterGoal,
            user.ProteinTarget, user.FatTarget, user.CarbTarget, user.FiberTarget,
            new UserCalorieSchedule(user.DailyCalorieTarget, user.CalorieCyclingEnabled,
                user.MondayCalories, user.TuesdayCalories, user.WednesdayCalories, user.ThursdayCalories,
                user.FridayCalories, user.SaturdayCalories, user.SundayCalories), user.TimeZoneId), cancellationToken);

    public Task<Result<UserGamificationProfileModel>> GetGamificationProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserGamificationProfileModel(new UserCalorieSchedule(
            user.DailyCalorieTarget, user.CalorieCyclingEnabled, user.MondayCalories, user.TuesdayCalories,
            user.WednesdayCalories, user.ThursdayCalories, user.FridayCalories, user.SaturdayCalories,
            user.SundayCalories)), cancellationToken);

    public async Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken) {
        UserDietologistProfileModel? profile = await FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return profile is null ? Result.Failure<UserDietologistProfileModel>(Errors.Authentication.InvalidToken) : Result.Success(profile);
    }

    public Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        DietologistProfiles(AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)).FirstOrDefaultAsync(cancellationToken);

    public Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        DietologistProfiles(AccessibleUsers.AsNoTracking().Where(user => user.Email == email)).FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<UserDietologistProfileModel> DietologistProfiles(IQueryable<User> users) =>
        users.AsNoTracking().Select(user => new UserDietologistProfileModel(user.Id.Value, user.Email, user.FirstName,
            user.LastName, user.Language, user.UserRoles.Any(role => role.Role.Name == RoleNames.Dietologist)));

    public Task<Result<UserHydrationProfileModel>> GetHydrationProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserHydrationProfileModel(user.HydrationGoal ?? user.WaterGoal), cancellationToken);

    public Task<Result<UserTdeeProfileModel>> GetTdeeProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserTdeeProfileModel(
            User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender),
            User.CalculateEstimatedTdee(User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender), user.ActivityLevel),
            user.WeightKg, user.DesiredWeightKg, user.DailyCalorieTarget), cancellationToken);

    public Task<Result<UserWeeklyCheckInProfileModel>> GetWeeklyCheckInProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
        ReadAsync(userId, user => new UserWeeklyCheckInProfileModel(user.DailyCalorieTarget), cancellationToken);

    private async Task<Result<T>> ReadAsync<T>(UserId userId, Expression<Func<User, T>> projection, CancellationToken cancellationToken) where T : class {
        T? profile = await AccessibleUsers.AsNoTracking().Where(user => user.Id == userId).Select(projection)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return profile is null ? Result.Failure<T>(Errors.Authentication.InvalidToken) : Result.Success(profile);
    }
}
