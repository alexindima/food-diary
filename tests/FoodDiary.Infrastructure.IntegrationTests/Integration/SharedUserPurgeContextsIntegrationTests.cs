using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Cycles.Infrastructure;
using FoodDiary.Modules.Hydration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedUserPurgeContextsIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OwnerPurge_RollsBackAndReusesScopeWithCurrentTransactionAsync(bool resolveBeforeTransaction) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser target = Seed(central, "target");
        SeededUser survivor = Seed(central, "survivor");
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[]? participants = resolveBeforeTransaction ? Participants(provider) : null;

        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            participants ??= Participants(provider);
            await PurgeAsync(participants, target.User.Id);
            await AssertDataAsync(central, target, exists: false);
            await AssertDataAsync(central, survivor, exists: true);
            await transaction.RollbackAsync();
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, target, exists: true);
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await PurgeAsync(participants, target.User.Id);
            await transaction.CommitAsync();
        }

        await AssertDataAsync(verification, target, exists: false);
        await AssertDataAsync(verification, survivor, exists: true);
        Assert.True(await verification.Users.AnyAsync(user => user.Id == target.User.Id));
        // Entry cleanup preserves replay receipts until the Users owner deletes the user row.
        Assert.True(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == target.User.Id));
    }

    [RequiresDockerFact]
    public async Task Cleanup_ContinuesWithNextUserAfterOwnerDeletionRollsBackAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser failed = Seed(central, "failed");
        SeededUser removed = Seed(central, "removed");
        SeededUser survivor = Seed(central, "survivor");
        failed.User.MarkDeleted(DateTime.UtcNow.AddDays(-20));
        removed.User.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[] participants = [.. Participants(provider), new FailingParticipant(failed.User.Id)];
        var service = new UserCleanupService(central, participants, NullLogger<UserCleanupService>.Instance,
            provider.GetRequiredService<IUnitOfWork>());

        Assert.Equal(1, await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null));

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, failed, exists: true);
        await AssertDataAsync(verification, removed, exists: false);
        await AssertDataAsync(verification, survivor, exists: true);
        Assert.True(await verification.Users.AnyAsync(user => user.Id == failed.User.Id));
        Assert.False(await verification.Users.AnyAsync(user => user.Id == removed.User.Id));
        Assert.True(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == failed.User.Id));
        Assert.False(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == removed.User.Id));
    }

    [RequiresDockerFact]
    public async Task OwnerPurge_CancellationPreservesDataAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser target = Seed(central, "canceled");
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[] participants = Participants(provider);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        foreach (IUserDataPurgeParticipant participant in participants) {
            await using IDbContextTransaction transaction = await central.Database.BeginTransactionAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                participant.PurgeAsync(target.User.Id, reassignTarget: null, cancellation.Token));
            await transaction.RollbackAsync();
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, target, exists: true);
    }

    private static SeededUser Seed(FoodDiaryDbContext context, string name) {
        var user = User.Create($"purge-{name}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var entry = HydrationEntry.Create(user.Id, now, 250);
        var profile = CycleProfile.Create(user.Id, today.AddDays(-28));
        profile.UpsertBleedingEntry(today.AddDays(-3), BleedingType.Bleeding, CycleFlowLevel.Medium, 4, "start");
        profile.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 3, ["lower"], "minor");
        profile.UpsertFactor(CycleFactorType.NonHormonalContraception, today.AddDays(-5), today.AddDays(-1), "tracking");
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, now);
        profile.UpsertFertilitySignal(today, 36.7, OvulationTestResult.Negative, "sticky", hadSex: false, notes: "signal");
        profile.ConfirmPeriodStart(today.AddDays(-3));
        context.AddRange(user, entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry),
            WeightEntry.Create(user.Id, now, 72.5), WaistEntry.Create(user.Id, now, 84), profile);
        return new SeededUser(user, profile.Id);
    }

    private static async Task AssertDataAsync(FoodDiaryDbContext context, SeededUser seeded, bool exists) {
        Assert.Equal(exists, await context.HydrationEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.WeightEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.WaistEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.CycleProfiles.AnyAsync(item => item.Id == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleBleedingEntries.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleSymptomEntries.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleFactors.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.FertilitySignals.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleMenstrualEpisodes.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.Set<CycleConsent>().AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
    }

    private static async Task PurgeAsync(IEnumerable<IUserDataPurgeParticipant> participants, UserId userId) {
        foreach (IUserDataPurgeParticipant participant in participants) {
            await participant.PurgeAsync(userId, reassignTarget: null, CancellationToken.None);
        }
    }

    private static IUserDataPurgeParticipant[] Participants(ServiceProvider provider) {
        IUserDataPurgeParticipant[] participants = [.. provider.GetServices<IUserDataPurgeParticipant>().OrderBy(item => item.Order)];
        Assert.Equal([80, 90, 100], participants.Select(item => item.Order));
        return participants;
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext central) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(central);
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddHydrationModule().AddBodyMetricsModule().AddCyclesModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed record SeededUser(User User, CycleProfileId ProfileId);

    [ExcludeFromCodeCoverage]
    private sealed class FailingParticipant(UserId target) : IUserDataPurgeParticipant {
        public int Order => 110;

        public Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) =>
            userId == target ? throw new InvalidOperationException("Injected failure after the owner deletions.") : Task.CompletedTask;
    }
}
