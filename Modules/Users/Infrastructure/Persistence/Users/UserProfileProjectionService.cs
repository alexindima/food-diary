using FoodDiary.Application.Abstractions.Authentication.Common;
using System.Linq.Expressions;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

public sealed class UserProfileProjectionService(DbSet<User> users, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) :
    ICurrentUserAccessService, IUserAiProfileReadService, IUserDashboardProfileReadService,
    IUserDietologistProfileReadService, IUserGamificationProfileReadService,
    IUserHydrationProfileReadService, IUserTdeeProfileReadService, IUserWeeklyCheckInProfileReadService,
    IUserBillingProfileReadModelRepository {
    private IQueryable<User> AccessibleUsers => users.AsNoTracking()
        .Where(user => user.IsActive && user.DeletedAt == null);

    public async Task<UserBillingProfileModel?> GetBillingProfileIncludingDeletedAsync(
        UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new UserBillingProfileModel(user.Id, user.Email, user.IsActive,
                user.DeletedAt != null, user.UserRoles.Any(role => role.Role.Name == RoleNames.Premium),
                user.PremiumTrialStartedAtUtc, user.PremiumTrialEndsAtUtc, user.IsEmailConfirmed))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await AccessibleUsers.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken).ConfigureAwait(false)
            ? null : AuthenticationErrors.InvalidToken;
    }

    public async Task<Result<UserAiProfileModel>> GetAiProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserAiProfileModel(user.Id, user.Language,
            user.AiInputTokenLimit, user.AiOutputTokenLimit, user.AiConsentAcceptedAt != null), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserDashboardProfileModel>> GetDashboardProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserDashboardProfileModel(user.Id.Value, user.Email, user.Language,
            user.DashboardLayoutJson, user.DesiredWeightKg, user.DesiredWaistCm, user.HydrationGoal, user.WaterGoal,
            user.ProteinTarget, user.FatTarget, user.CarbTarget, user.FiberTarget,
            new UserCalorieSchedule(user.DailyCalorieTarget, user.CalorieCyclingEnabled,
                user.MondayCalories, user.TuesdayCalories, user.WednesdayCalories, user.ThursdayCalories,
                user.FridayCalories, user.SaturdayCalories, user.SundayCalories), user.TimeZoneId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserGamificationProfileModel>> GetGamificationProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserGamificationProfileModel(new UserCalorieSchedule(
            user.DailyCalorieTarget, user.CalorieCyclingEnabled, user.MondayCalories, user.TuesdayCalories,
            user.WednesdayCalories, user.ThursdayCalories, user.FridayCalories, user.SaturdayCalories,
            user.SundayCalories)), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserDietologistProfileModel>> GetAccessibleProfileAsync(UserId userId, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        UserDietologistProfileModel? profile = await FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return profile is null ? Result.Failure<UserDietologistProfileModel>(AuthenticationErrors.InvalidToken) : Result.Success(profile);
    }

    public async Task<UserDietologistProfileModel?> FindByIdAsync(UserId userId, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await DietologistProfiles(AccessibleUsers.AsNoTracking().Where(user => user.Id == userId)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDietologistProfileModel?> FindByEmailAsync(string email, CancellationToken cancellationToken) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await DietologistProfiles(AccessibleUsers.AsNoTracking().Where(user => user.Email == email)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IQueryable<UserDietologistProfileModel> DietologistProfiles(IQueryable<User> users) =>
        users.AsNoTracking().Select(user => new UserDietologistProfileModel(user.Id.Value, user.Email, user.FirstName,
            user.LastName, user.Language, user.UserRoles.Any(role => role.Role.Name == RoleNames.Dietologist)));

    public async Task<Result<UserHydrationProfileModel>> GetHydrationProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserHydrationProfileModel(user.HydrationGoal ?? user.WaterGoal), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserTdeeProfileModel>> GetTdeeProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserTdeeProfileModel(
            User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender),
            User.CalculateEstimatedTdee(User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender), user.ActivityLevel),
            user.WeightKg, user.DesiredWeightKg, user.DailyCalorieTarget), cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<UserWeeklyCheckInProfileModel>> GetWeeklyCheckInProfileAsync(UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await ReadAsync(userId, user => new UserWeeklyCheckInProfileModel(user.DailyCalorieTarget), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<T>> ReadAsync<T>(UserId userId, Expression<Func<User, T>> projection, CancellationToken cancellationToken) where T : class {
        T? profile = await AccessibleUsers.AsNoTracking().Where(user => user.Id == userId).Select(projection)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return profile is null ? Result.Failure<T>(AuthenticationErrors.InvalidToken) : Result.Success(profile);
    }
}
