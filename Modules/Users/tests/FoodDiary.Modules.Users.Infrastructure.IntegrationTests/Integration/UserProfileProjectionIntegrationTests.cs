using System.Data.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserProfileProjectionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ConsumerProfiles_ProjectOnlyRequiredValues() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("feature-profiles@example.com", "hash");
        user.UpdatePersonalInfo(birthDate: new DateTime(1990, 3, 1, 0, 0, 0, DateTimeKind.Utc), gender: "M", weight: 80, height: 180);
        user.AcceptAiConsent();
        user.SetTimeZone("Asia/Tbilisi");
        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: 2050,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            WaterGoal: 2.4,
            DesiredWeightKg: 71,
            DesiredWaistCm: null));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new UserProfileProjectionService(context);

        UserAiProfileModel ai = Success(await service.GetAiProfileAsync(user.Id, CancellationToken.None));
        UserHydrationProfileModel hydration = Success(await service.GetHydrationProfileAsync(user.Id, CancellationToken.None));
        UserTdeeProfileModel tdee = Success(await service.GetTdeeProfileAsync(user.Id, CancellationToken.None));
        UserWeeklyCheckInProfileModel weeklyCheckIn = Success(
            await service.GetWeeklyCheckInProfileAsync(user.Id, CancellationToken.None));
        UserDashboardProfileModel dashboard = Success(
            await service.GetDashboardProfileAsync(user.Id, CancellationToken.None));
        UserGamificationProfileModel gamification = Success(
            await service.GetGamificationProfileAsync(user.Id, CancellationToken.None));
        UserDietologistProfileModel dietologist = Success(
            await service.GetAccessibleProfileAsync(user.Id, CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(user.Id, ai.UserId),
            () => Assert.Equal(user.Language, ai.Language),
            () => Assert.Equal(user.AiInputTokenLimit, ai.InputTokenLimit),
            () => Assert.Equal(user.AiOutputTokenLimit, ai.OutputTokenLimit),
            () => Assert.True(ai.HasAcceptedAiConsent),
            () => Assert.Equal(user.HydrationGoal ?? user.WaterGoal, hydration.EffectiveWaterGoal),
            () => Assert.Equal(user.CalculateBmr(), tdee.Bmr),
            () => Assert.Equal(user.CalculateEstimatedTdee(), tdee.EstimatedTdee),
            () => Assert.Equal(user.WeightKg, tdee.WeightKg),
            () => Assert.Equal(user.DesiredWeightKg, tdee.DesiredWeightKg),
            () => Assert.Equal(user.DailyCalorieTarget, tdee.DailyCalorieTarget),
            () => Assert.Equal(user.DailyCalorieTarget, weeklyCheckIn.DailyCalorieTarget),
            () => Assert.Equal(user.Email, dashboard.Email),
            () => Assert.Equal("Asia/Tbilisi", dashboard.TimeZoneId),
            () => Assert.Equal(2050, dashboard.GetCalorieTargetForDate(new DateOnly(2026, 9, 12))),
            () => Assert.Equal(user.DashboardLayoutJson, dashboard.DashboardLayoutJson),
            () => Assert.Equal(user.DailyCalorieTarget, dashboard.CalorieSchedule.GetTargetForDate(DateTime.UtcNow)),
            () => Assert.Equal(user.DailyCalorieTarget, gamification.CalorieSchedule.GetTargetForDate(DateTime.UtcNow)),
            () => Assert.Equal(user.Email, dietologist.Email),
            () => Assert.Equal(user.FirstName, dietologist.FirstName),
            () => Assert.False(dietologist.IsDietologist));
        Assert.NotNull(tdee.Bmr);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task ConsumerProfiles_WhenUserIsMissing_ReturnAccessFailures() {
        var userId = UserId.New();
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var service = new UserProfileProjectionService(context);

        Result<UserAiProfileModel> ai = await service.GetAiProfileAsync(userId, CancellationToken.None);
        Result<UserDashboardProfileModel> dashboard = await service.GetDashboardProfileAsync(userId, CancellationToken.None);
        Result<UserTdeeProfileModel> tdee = await service.GetTdeeProfileAsync(userId, CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal("Authentication.InvalidToken", Failure(ai).Code),
            () => Assert.Equal("Authentication.InvalidToken", Failure(dashboard).Code),
            () => Assert.Equal("Authentication.InvalidToken", Failure(tdee).Code));
    }

    [RequiresDockerFact]
    public async Task ConsumerProfiles_UsePersistedStateAndRejectInactiveOrDeletedAccounts() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("profile-active@example.com", "hash");
        active.UpdateActivity(hydrationGoal: 2.5);
        var inactive = User.Create("profile-inactive@example.com", "hash");
        inactive.Deactivate();
        var deleted = User.Create("profile-deleted@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        active.UpdateActivity(hydrationGoal: 9);
        active.Deactivate();
        var service = new UserProfileProjectionService(context);

        Assert.Null(await service.EnsureCanAccessAsync(active.Id));
        Assert.Equal(2.5, Success(await service.GetHydrationProfileAsync(active.Id)).EffectiveWaterGoal);
        foreach (User excluded in new[] { inactive, deleted }) {
            Assert.Equal("Authentication.InvalidToken", (await service.EnsureCanAccessAsync(excluded.Id))?.Code);
            Assert.True((await service.GetDashboardProfileAsync(excluded.Id)).IsFailure);
            Assert.Null(await service.FindByIdAsync(excluded.Id, CancellationToken.None));
            Assert.NotNull(excluded.Email);
            Assert.Null(await service.FindByEmailAsync(excluded.Email, CancellationToken.None));
        }
        Assert.Equal(9, active.HydrationGoal);
        Assert.True(context.ChangeTracker.HasChanges());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetHydrationProfileAsync(active.Id, cancelled.Token));
    }

    [RequiresDockerFact]
    public async Task HydrationProjection_UsesOneNarrowQueryWithoutCredentialsRolesOrGoals() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("narrow-profile@example.com", "hash");
        user.UpdateActivity(hydrationGoal: 2.5);
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        var capture = new QueryCapture();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString()).AddInterceptors(capture).Options;
        await using var context = new FoodDiaryDbContext(options);
        Result<UserHydrationProfileModel> result = await new UserProfileProjectionService(context).GetHydrationProfileAsync(user.Id);

        Assert.Equal(2.5, Success(result).EffectiveWaterGoal);
        string sql = Assert.Single(capture.Commands);
        Assert.Contains("HydrationGoal", sql, StringComparison.Ordinal);
        foreach (string unrelated in new[] { "Password", "UserRoles", "WeightGoals", "WaistGoals" }) {
            Assert.DoesNotContain(unrelated, sql, StringComparison.Ordinal);
        }
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [ExcludeFromCodeCoverage]
    private sealed class QueryCapture : DbCommandInterceptor {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private static T Success<T>(Result<T> result) {
        Assert.True(result.IsSuccess, result.Error.Message);
        return result.Value;
    }
    private static Error Failure(Result result) {
        Assert.True(result.IsFailure);
        return result.Error;
    }
}
