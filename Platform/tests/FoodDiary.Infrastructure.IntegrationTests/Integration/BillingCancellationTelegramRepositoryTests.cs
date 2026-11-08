using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BillingCancellationTelegramRepositoryTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TelegramMarker_IsOwnerScopedIndependentAndDoesNotOverwriteConcurrentCancellation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"telegram-notice-{Guid.NewGuid():N}@example.test", "hash");
        var other = User.Create($"telegram-notice-other-{Guid.NewGuid():N}@example.test", "hash");
        var now = new DateTime(2026, 11, 8, 11, 12, 45, DateTimeKind.Utc);
        BillingCancellation Create(string provider, string externalId, DateTime requested) {
            var subscription = BillingSubscription.CreatePending(user.Id, provider, "customer", "price", "monthly");
            subscription.ApplyProviderSnapshot(provider, externalId, externalPaymentMethodId: null, "price", "monthly", "active", now,
                now.AddDays(30), cancelAtPeriodEnd: true, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                "cancel", now, webhookOccurredAtUtc: now);
            return BillingCancellation.Create(subscription, "fooddiary", requested, now.AddDays(30));
        }
        BillingCancellation cancellation = Create(BillingProviderNames.TelegramStars, "stars-owned", now);
        cancellation.MarkConfirmationEmailHandled(now);
        BillingCancellation paddle = Create(BillingProviderNames.Paddle, "paddle-owned", now.AddMinutes(-1));
        context.Users.AddRange(user, other);
        context.BillingCancellations.AddRange(cancellation, paddle);
        await context.SaveChangesAsync();
        int synchronizations = 0;
        var repository = new BillingCancellationRepository(context.BillingCancellations, _ => { synchronizations++; return Task.CompletedTask; });

        Assert.Equal(cancellation.TypedId, Assert.Single(await repository.GetPendingTelegramConfirmationsAsync(100, CancellationToken.None)).Id);
        Assert.Null(await repository.GetTelegramConfirmationAsync(cancellation.TypedId, other.Id, CancellationToken.None));
        await repository.MarkTelegramConfirmationHandledAsync(cancellation.TypedId, other.Id, now, CancellationToken.None);
        Assert.Null((await repository.GetTelegramConfirmationAsync(cancellation.TypedId, user.Id, CancellationToken.None))!.HandledAtUtc);

        // Keep the old tracked aggregate, as a worker can while Telegram HTTP is in flight.
        // Simulate a separately committed resumption/feedback update before marking delivery.
        await context.BillingCancellations.Where(item => item.Id == cancellation.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Outcome, "cancellation_removed")
                .SetProperty(item => item.Comment, "Concurrent feedback"));
        Assert.Equal("scheduled", cancellation.Outcome);
        Assert.Equal("cancellation_removed", (await repository.GetTelegramConfirmationAsync(cancellation.TypedId, user.Id, CancellationToken.None))!.Outcome);

        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            await repository.MarkTelegramConfirmationHandledAsync(cancellation.TypedId, user.Id, now.AddMinutes(1), CancellationToken.None);
            await transaction.RollbackAsync();
        }
        Assert.Null((await repository.GetTelegramConfirmationAsync(cancellation.TypedId, user.Id, CancellationToken.None))!.HandledAtUtc);
        await repository.MarkTelegramConfirmationHandledAsync(cancellation.TypedId, user.Id, now.AddMinutes(2), CancellationToken.None);
        await repository.MarkTelegramConfirmationHandledAsync(cancellation.TypedId, user.Id, now.AddMinutes(3), CancellationToken.None);

        BillingCancellation stored = await context.BillingCancellations.AsNoTracking().SingleAsync(item => item.Id == cancellation.Id);
        Assert.Equal(now.AddMinutes(2), stored.ConfirmationTelegramHandledAtUtc);
        Assert.Equal(now, stored.ConfirmationEmailHandledAtUtc);
        Assert.Equal("cancellation_removed", stored.Outcome);
        Assert.Equal("Concurrent feedback", stored.Comment);
        Assert.Equal(now.AddDays(30), stored.EffectiveAtUtc);
        Assert.Empty(await repository.GetPendingTelegramConfirmationsAsync(100, CancellationToken.None));
        Assert.Single(await repository.GetPendingConfirmationsAsync(100, CancellationToken.None));
        Assert.True(synchronizations >= 10);
    }
}
