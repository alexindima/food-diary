using FoodDiary.Domain.Primitives;
using FoodDiary.Testing.Assertions;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserGoalHistoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SavedHistory_UsesLocalDateAndLastSameDayChange_WithoutTrackingOrLoadingFullHistory() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        User user;
        using (DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)))) {
            user = User.Create("history-roundtrip@example.com", "hash");
            user.SetTimeZone("Asia/Tbilisi");
            user.UpdateGoals(new UserGoalUpdate(DailyCalorieTarget: 3000, ProteinTarget: 150, FatTarget: 90,
                CarbTarget: 350, FiberTarget: 30, WaterGoal: 2000, CalorieCyclingEnabled: true, MondayCalories: 3200));
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        context.ChangeTracker.Clear();
        user = await context.Users.SingleAsync();
        Assert.Empty(user.NutritionProfile.GoalHistory);
        using (DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 10, 29, 22, 0, 0, TimeSpan.Zero)))) {
            user.UpdateGoals(dailyCalorieTarget: 2500, proteinTarget: 120);
            user.UpdateActivity(hydrationGoal: 2400);
            await context.SaveChangesAsync();
            user.UpdateGoals(dailyCalorieTarget: 2600);
            await context.SaveChangesAsync();
        }
        context.ChangeTracker.Clear();
        var service = new UserProfileProjectionService(context.Users);
        UserDashboardProfileModel profile = ResultAssert.Success(await service.GetDashboardProfileAsync(user.Id,
            CancellationToken.None, new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 31)));
        UserGoalTimeline timeline = Assert.IsType<UserGoalTimeline>(profile.GoalHistory);
        Assert.Equal(2, timeline.Versions.Count);
        UserDailyGoals before = profile.GetGoalsForDate(new DateOnly(2026, 10, 29));
        UserDailyGoals after = profile.GetGoalsForDate(new DateOnly(2026, 10, 30));
        Assert.Multiple(
            () => Assert.Equal(3000, before.CalorieSchedule.DailyCalorieTarget),
            () => Assert.Equal(150, before.ProteinTarget),
            () => Assert.Equal(2000, before.EffectiveWaterGoal),
            () => Assert.Equal(3200, before.CalorieSchedule.MondayCalories),
            () => Assert.Equal(2600, after.CalorieSchedule.DailyCalorieTarget),
            () => Assert.Equal(120, after.ProteinTarget),
            () => Assert.Equal(2400, after.EffectiveWaterGoal),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        UserHydrationProfileModel historicalWater = ResultAssert.Success(await service.GetHydrationProfileAsync(user.Id,
            CancellationToken.None, new DateOnly(2026, 10, 29)));
        Assert.Equal(2000, historicalWater.EffectiveWaterGoal);
        UserGamificationProfileModel adherence = ResultAssert.Success(await service.GetGamificationProfileAsync(user.Id,
            CancellationToken.None, new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30)));
        Assert.Equal(3000, adherence.GetCalorieTargetForDate(new DateTime(2026, 10, 29)));
        UserWeeklyCheckInProfileModel checkIn = ResultAssert.Success(await service.GetWeeklyCheckInProfileAsync(user.Id,
            CancellationToken.None, new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30)));
        Assert.Equal(2800, checkIn.GetAverageCalorieTarget(new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30)));
        Assert.False(context.Database.HasPendingModelChanges());
        user = await context.Users.SingleAsync();
        user.Deactivate();
        await context.SaveChangesAsync();
        ResultAssert.Failure(await service.GetDashboardProfileAsync(user.Id, CancellationToken.None,
            new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30)));
    }

    [RequiresDockerFact]
    public async Task DetachedGoalUpdate_WithOriginalConcurrencyVersions_AppendsNewSnapshots() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("history-detached@example.com", "hash");
        user.UpdateGoals(dailyCalorieTarget: 3000);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        uint accountVersion = context.Entry(user).Property<uint>("xmin").CurrentValue;
        uint nutritionVersion = context.Entry(user.NutritionProfile).Property<uint>("xmin").CurrentValue;
        uint preferencesVersion = context.Entry(user.Preferences).Property<uint>("xmin").CurrentValue;
        context.ChangeTracker.Clear();
        user.UpdateGoals(dailyCalorieTarget: 2500);
        var repository = new UserRepository(context.Users, context.UserRoleAuditEvents);
        await repository.UpdateAsync(user);
        // A detached caller must retain the existing shadow concurrency tokens for every profile partition.
        context.Entry(user).Property<uint>("xmin").OriginalValue = accountVersion;
        context.Entry(user.NutritionProfile).Property<uint>("xmin").OriginalValue = nutritionVersion;
        context.Entry(user.Preferences).Property<uint>("xmin").OriginalValue = preferencesVersion;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(3, await context.Set<UserGoalRevision>().CountAsync());
        Assert.Equal(3000, await context.Set<UserGoalRevision>().Where(revision => revision.Revision == 1)
            .Select(revision => revision.DailyCalorieTarget).SingleAsync());
        Assert.Equal(2500, await context.Users.Select(account => account.NutritionProfile.DailyCalorieTarget).SingleAsync());
    }

    [RequiresDockerFact]
    public async Task ConflictingGoalSave_RollsBackItsHistoryTogetherWithTheProfile() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("history-concurrency@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        string connectionString = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connectionString);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connectionString);
        User firstUser = await first.Users.SingleAsync();
        User secondUser = await second.Users.SingleAsync();
        firstUser.UpdateGoals(dailyCalorieTarget: 2500);
        secondUser.UpdateGoals(dailyCalorieTarget: 3000);
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        seed.ChangeTracker.Clear();
        Assert.Equal(2500, await seed.Users.Select(account => account.NutritionProfile.DailyCalorieTarget).SingleAsync());
        Assert.Equal(2, await seed.Set<UserGoalRevision>().CountAsync());
        Assert.DoesNotContain(await seed.Set<UserGoalRevision>().ToListAsync(), revision => revision.DailyCalorieTarget == 3000);
    }

    [RequiresDockerFact]
    public async Task Migration_BackfillsActualCurrentValuesAndNulls_AndDownPreservesCurrentProfile() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var configured = User.Create("history-migration@example.com", "hash");
        configured.UpdateGoals(new UserGoalUpdate(DailyCalorieTarget: 3000, ProteinTarget: 150, FatTarget: 90,
            CarbTarget: 350, FiberTarget: 30, CalorieCyclingEnabled: true, MondayCalories: 3200));
        configured.UpdateActivity(stepGoal: 10000, hydrationGoal: 2400);
        var empty = User.Create("history-migration-null@example.com", "hash");
        context.Users.AddRange(configured, empty);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261026035544_EnableBrowserSessionRenewal");
        await context.Database.MigrateAsync();

        var service = new UserProfileProjectionService(context.Users);
        UserDashboardProfileModel profile = ResultAssert.Success(await service.GetDashboardProfileAsync(configured.Id,
            CancellationToken.None, new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2)));
        UserGoalVersion baseline = Assert.Single(Assert.IsType<UserGoalTimeline>(profile.GoalHistory).Versions);
        Assert.Multiple(
            () => Assert.Equal(0, baseline.Revision),
            () => Assert.Equal(DateOnly.MinValue, baseline.EffectiveFrom),
            () => Assert.Equal(configured.NutritionProfile.GetDailyGoals(), baseline.Goals));
        UserDashboardProfileModel nullProfile = ResultAssert.Success(await service.GetDashboardProfileAsync(empty.Id,
            CancellationToken.None, new DateOnly(2020, 1, 1)));
        Assert.Null(nullProfile.GetGoalsForDate(new DateOnly(2020, 1, 1)).EffectiveWaterGoal);
        Assert.Null(nullProfile.GetCalorieTargetForDate(new DateOnly(2020, 1, 1)));
        configured = await context.Users.SingleAsync(account => account.Id == configured.Id);
        configured.UpdateGoals(dailyCalorieTarget: 2500);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.Users.Where(account => account.Id == configured.Id).Select(account => account.NutritionProfile.GoalRevision).SingleAsync());
        await context.Users.Where(account => account.Id == configured.Id).ExecuteDeleteAsync();
        Assert.Equal(1, await context.Set<UserGoalRevision>().CountAsync());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
