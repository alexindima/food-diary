using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BillingCancellationRepositoryBoundaryTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task LatestCancellation_FiltersOwnerProviderAndSubscriptionAndUsesStableOrdering() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"cancel-owner-{Guid.NewGuid():N}@example.test", "hash");
        var other = User.Create($"cancel-other-{Guid.NewGuid():N}@example.test", "hash");
        context.Users.AddRange(owner, other);
        DateTime now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        BillingCancellation Create(User user, string provider, string externalId, DateTime requested) {
            var subscription = BillingSubscription.CreatePending(user.Id, provider, "customer", "price", "monthly");
            subscription.ApplyProviderSnapshot(provider, externalId, externalPaymentMethodId: null, "price", "monthly", "active", now,
                now.AddDays(30), cancelAtPeriodEnd: true, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt", now, webhookOccurredAtUtc: now);
            return BillingCancellation.Create(subscription, "fooddiary", requested, now.AddDays(30));
        }
        BillingCancellation first = Create(owner, BillingProviderNames.Paddle, "owned", now);
        BillingCancellation second = Create(owner, BillingProviderNames.Paddle, "owned", now);
        int synchronizations = 0;
        var repository = new BillingCancellationRepository(context.BillingCancellations,
            _ => { synchronizations++; return Task.CompletedTask; });
        foreach (BillingCancellation row in new[] { first, second,
            Create(owner, BillingProviderNames.Paddle, "owned", now.AddMinutes(-1)),
            Create(other, BillingProviderNames.Paddle, "owned", now.AddMinutes(1)),
            Create(owner, BillingProviderNames.Stripe, "owned", now.AddMinutes(1)),
            Create(owner, BillingProviderNames.Paddle, "foreign", now.AddMinutes(1)), }) {
            await repository.AddAsync(row, CancellationToken.None);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        BillingCancellation? latest = await repository.GetLatestAsync(owner.Id, BillingProviderNames.Paddle, "owned", CancellationToken.None);

        Assert.NotNull(latest);
        Guid expected = await context.BillingCancellations.Where(item => item.Id == first.Id || item.Id == second.Id)
            .OrderByDescending(item => item.Id).Select(item => item.Id).FirstAsync();
        Assert.Equal(expected, latest.Id);
        Assert.Null(await repository.GetLatestAsync(owner.Id, BillingProviderNames.Paddle, "absent", CancellationToken.None));
        Assert.Equal(2, synchronizations);
    }
}
