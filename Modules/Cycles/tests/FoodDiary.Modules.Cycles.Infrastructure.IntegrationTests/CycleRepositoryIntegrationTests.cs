using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests;

#pragma warning disable MA0004

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class CycleRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Repository_CoversDetailsTrackingCurrentAndUserQueries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"cycle-repo-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var repository = new CycleRepository(context);
        var profile = CycleProfile.Create(
            user.Id,
            today.AddDays(-28),
            CycleTrackingMode.TryingToConceive,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 13,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: false,
            notes: "Initial");
        AddCycleDetails(profile, today);
        await repository.AddAsync(profile);
        await context.SaveChangesAsync();

        CycleProfile? tracked = await repository.GetByIdAsync(profile.Id, user.Id, includeDetails: true, asTracking: true);
        Assert.NotNull(tracked);
        tracked.UpdateSettings(new CycleProfileSettings(
            tracked.Mode,
            tracked.AverageCycleLength,
            tracked.AveragePeriodLength,
            tracked.LutealLength,
            tracked.IsRegular,
            tracked.IsOnboardingComplete,
            tracked.ShowFertilityEstimates,
            tracked.DiscreetNotifications,
            Notes: "Updated"));
        tracked.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 5, ["mild"], "updated");
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();

        CycleProfile? byId = await repository.GetByIdAsync(profile.Id, user.Id, includeDetails: true);
        CycleProfile? current = await repository.GetCurrentAsync(user.Id, includeDetails: true);
        IReadOnlyList<CycleProfile> profiles = await repository.GetByUserAsync(user.Id, includeDetails: true);

        Assert.Equal("Updated", byId?.Notes);
        Assert.NotEmpty(byId!.BleedingEntries);
        Assert.NotEmpty(byId.SymptomEntries);
        Assert.NotEmpty(byId.Factors);
        Assert.NotEmpty(byId.FertilitySignals);
        Assert.Equal(profile.Id, current?.Id);
        Assert.Equal(profile.Id, Assert.Single(profiles).Id);
    }

    [RequiresDockerFact]
    public async Task Delete_RemovesOnlyCycleAggregate() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"cycle-delete-{Guid.NewGuid():N}@example.com", "hash");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, today.AddDays(-28));
        AddCycleDetails(profile, today);
        profile.ConfirmPeriodStart(today.AddDays(-3));
        var meal = Meal.Create(user.Id, today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), MealType.Dinner);
        context.Users.Add(user);
        context.Meals.Add(meal);
        var repository = new CycleRepository(context);
        await repository.AddAsync(profile);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        CycleProfile? tracked = await repository.GetByIdAsync(profile.Id, user.Id, includeDetails: false, asTracking: true);
        Assert.NotNull(tracked);
        await repository.DeleteAsync(tracked);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Multiple(
            () => Assert.False(context.CycleProfiles.Any(item => item.Id == profile.Id)),
            () => Assert.False(context.CycleBleedingEntries.Any(item => item.CycleProfileId == profile.Id)),
            () => Assert.False(context.CycleSymptomEntries.Any(item => item.CycleProfileId == profile.Id)),
            () => Assert.False(context.CycleFactors.Any(item => item.CycleProfileId == profile.Id)),
            () => Assert.False(context.FertilitySignals.Any(item => item.CycleProfileId == profile.Id)),
            () => Assert.False(context.CycleMenstrualEpisodes.Any(item => item.CycleProfileId == profile.Id)),
            () => Assert.True(context.Users.Any(item => item.Id == user.Id)),
            () => Assert.True(context.Meals.Any(item => item.Id == meal.Id)));
    }

    [RequiresDockerFact]
    public async Task CurrentReadModel_DoesNotUseSingleQueryForMultipleCollections() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .Options;
        await using var context = new FoodDiaryDbContext(options);
        await context.Database.MigrateAsync();

        var user = User.Create($"cycle-read-model-{Guid.NewGuid():N}@example.com", "hash");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profile = CycleProfile.Create(
            user.Id,
            today.AddDays(-28),
            CycleTrackingMode.TryingToConceive,
            averageCycleLength: 30,
            averagePeriodLength: 6,
            lutealLength: 13,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: false,
            notes: "Initial");
        AddCycleDetails(profile, today);
        context.Users.Add(user);
        context.CycleProfiles.Add(profile);
        await context.SaveChangesAsync();

        var repository = new CycleRepository(context);
        CycleProfileReadModel? readModel = await repository.GetCurrentReadModelAsync(user.Id);

        Assert.NotNull(readModel);
        Assert.Multiple(
            () => Assert.Equal(2, readModel.BleedingEntries.Count),
            () => Assert.Single(readModel.SymptomEntries),
            () => Assert.Single(readModel.Factors),
            () => Assert.Single(readModel.FertilitySignals));
    }

    private static void AddCycleDetails(CycleProfile profile, DateOnly today) {
        profile.UpsertBleedingEntry(today.AddDays(-3), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 4, notes: "start");
        profile.UpsertBleedingEntry(today.AddDays(-2), BleedingType.Spotting, CycleFlowLevel.Light, painImpact: 1, notes: "spotting");
        profile.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 3, ["lower"], "minor");
        profile.UpsertFactor(CycleFactorType.NonHormonalContraception, today.AddDays(-5), today.AddDays(-1), "tracking");
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertFertilitySignal(today, 36.7, OvulationTestResult.Negative, "sticky", hadSex: false, notes: "signal");
    }
}
