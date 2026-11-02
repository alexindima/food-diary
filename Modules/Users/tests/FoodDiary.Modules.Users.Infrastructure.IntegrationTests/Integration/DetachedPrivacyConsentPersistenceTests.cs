using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Testing.Assertions;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DetachedPrivacyConsentPersistenceTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DetachedConsentHistory_PreservesConcurrencyAndTrackedRetryAppendsOnlyTheWithdrawal() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        DateTime now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"detached-consent-{Guid.NewGuid():N}@example.test", "hash");
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: true, PrivacyNoticeVersions.Current, now);
        UserPrivacyConsentEvent original = Assert.Single(user.PrivacyConsentEvents);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: false, PrivacyNoticeVersions.Current, now.AddMinutes(1));
        UserPrivacyConsentEvent withdrawal = Assert.Single(user.PrivacyConsentEvents, item => !item.Granted);

        await new UserRepository(context.Users, context.UserRoleAuditEvents).UpdateAsync(user);

        Assert.Multiple(
            () => Assert.Equal(EntityState.Unchanged, context.Entry(original).State),
            () => Assert.Equal(EntityState.Added, context.Entry(withdrawal).State));
        // A detached aggregate has lost its original shadow xmin values. Attaching
        // consent history must not bypass the existing optimistic concurrency fence.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        User retry = await context.Users.Include(account => account.PrivacyConsentEvents)
            .SingleAsync(account => account.Id == user.Id);
        Assert.Equal(original.Id, Assert.Single(retry.PrivacyConsentEvents).Id);
        Assert.Equal(now, retry.HealthConsentAcceptedAtUtc);
        retry.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: false, PrivacyNoticeVersions.Current, now.AddMinutes(1));
        UserPrivacyConsentEvent committedWithdrawal = Assert.Single(retry.PrivacyConsentEvents, item => !item.Granted);
        await new UserRepository(context.Users, context.UserRoleAuditEvents).UpdateAsync(retry);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        User persisted = await context.Users.AsNoTracking().Include(account => account.PrivacyConsentEvents)
            .SingleAsync(account => account.Id == user.Id);
        Assert.Multiple(
            () => Assert.Equal(2, persisted.PrivacyConsentEvents.Count),
            () => Assert.Null(persisted.HealthConsentAcceptedAtUtc),
            () => Assert.Equal(now.AddMinutes(1), persisted.HealthConsentDeclinedAtUtc),
            () => Assert.Equal(original.Id, Assert.Single(persisted.PrivacyConsentEvents, item => item.Granted).Id),
            () => Assert.Equal(committedWithdrawal.Id, Assert.Single(persisted.PrivacyConsentEvents, item => !item.Granted).Id));
    }

    [RequiresDockerFact]
    public async Task InvalidGoalRange_PreservesAccessFailureBeforeRejectingAnAccessibleUsersRange() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"goal-range-{Guid.NewGuid():N}@example.test", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        int synchronizations = 0;
        var profiles = new UserProfileProjectionService(context.Users, _ => { synchronizations++; return Task.CompletedTask; });
        DateOnly from = new(2026, 11, 2);

        ResultAssert.Failure(await profiles.GetDashboardProfileAsync(
            UserId.New(), CancellationToken.None, from, from.AddDays(-1)), "Authentication.InvalidToken");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => profiles.GetDashboardProfileAsync(user.Id, CancellationToken.None, from, from.AddDays(-1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => profiles.GetDashboardProfileAsync(user.Id, CancellationToken.None, from, from.AddDays(367)));
        Assert.Equal(3, synchronizations);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
