using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerFact]
    public async Task Checkout_ReplayedProviderSessionAfterCommit_DoesNotInsertAgainAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("checkout-replay@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        ISender users = WorkflowUsers(user);
        var profile = new UserBillingProfileModel(user.Id, user.Email, IsActive: true, IsDeleted: false, HasPaidPremium: false,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true);
        users.Send(new GetUserBillingProfileQuery(UserId: user.Id), Arg.Any<CancellationToken>()).Returns(Result.Success(profile));
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.Stripe);
        var session = new BillingCheckoutSessionModel("cs_replayed", "https://checkout.example/replayed", "cus_replayed", "price", "monthly");
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(session));
        IBillingProviderGatewayAccessor accessor = Substitute.For<IBillingProviderGatewayAccessor>();
        accessor.GetProviderOrDefault(BillingProviderNames.Stripe).Returns(gateway);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        var handler = new CreateCheckoutSessionCommandHandler(users,
            provider.GetRequiredService<IBillingSubscriptionWriteRepository>(),
            provider.GetRequiredService<IBillingPaymentWriteRepository>(), accessor, clock,
            provider.GetRequiredService<IBillingCheckoutLock>(), provider.GetRequiredService<IBillingTransactionRunner>());
        var command = new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.Stripe, "same_key");
        Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);
        BillingSubscription before = await context.BillingSubscriptions.AsNoTracking().SingleAsync();

        // Simulate the response cache being lost after the transaction committed.
        provider.GetRequiredService<FoodDiary.Modules.Billing.Infrastructure.Persistence.BillingDbContext>().ChangeTracker.Clear();
        clock.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddMinutes(30));
        Result<BillingCheckoutSessionModel> replay = await handler.Handle(command, CancellationToken.None);

        Assert.True(replay.IsSuccess);
        Assert.Equal(session, replay.Value);
        Assert.Equal(1, await context.BillingPayments.CountAsync());
        BillingSubscription after = await context.BillingSubscriptions.AsNoTracking().SingleAsync();
        Assert.Equal(before.CreatedOnUtc, after.CreatedOnUtc);
        Assert.Equal(before.ModifiedOnUtc, after.ModifiedOnUtc);
    }

    [RequiresDockerTheory]
    [InlineData(true, "active")]
    [InlineData(false, "active")]
    [InlineData(true, "canceled")]
    [InlineData(false, null)]
    [InlineData(true, null)]
    public async Task Checkout_ReloadsConcurrentWebhookFromAnotherContextAsync(bool hasSubscription, string? webhookStatus) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("checkout-concurrent@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        DateTime now = DateTime.UtcNow;
        await using ServiceProvider provider = CreateProvider(context);
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        if (hasSubscription) {
            await runner.ExecuteAsync(async token => {
                var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa, user.Id.Value.ToString(), "price", "monthly");
                subscription.UpdateCheckoutContext(BillingProviderNames.YooKassa, user.Id.Value.ToString(), "price", "monthly");
                subscription.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "old_payment", "pm_saved", "price", "monthly",
                    BillingSubscription.PendingCheckoutStatus, currentPeriodStartUtc: null, currentPeriodEndUtc: null,
                    cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                    webhookEventId: "checkout_old", syncedAtUtc: now.AddHours(-1), providerMetadataJson: null, webhookOccurredAtUtc: now.AddHours(-1));
                await subscriptions.AddAsync(subscription, token);
            });
        }
        provider.GetRequiredService<FoodDiary.Modules.Billing.Infrastructure.Persistence.BillingDbContext>().ChangeTracker.Clear();
        await using FoodDiaryDbContext otherContext = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider otherProvider = CreateProvider(otherContext);
        ISender users = WorkflowUsers(user);
        var profile = new UserBillingProfileModel(user.Id, user.Email, IsActive: true, IsDeleted: false, HasPaidPremium: false,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true);
        users.Send(new GetUserBillingProfileIncludingDeletedQuery(UserId: user.Id), Arg.Any<CancellationToken>()).Returns(profile);
        users.Send(new GetUserBillingProfileQuery(UserId: user.Id), Arg.Any<CancellationToken>()).Returns(Result.Success(profile));
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>()).Returns(async _ => {
            if (webhookStatus is not null) {
                BillingWebhookEventModel webhook = WorkflowWebhook(user, "concurrent_webhook", "paid_old_session", now) with { Status = webhookStatus };
                Assert.True((await WorkflowProcessor(otherProvider, users, Substitute.For<ISender>())
                    .ProcessAsync(BillingProviderNames.YooKassa, "{}", webhook, inboxEvent: null, CancellationToken.None)).IsSuccess);
            }
            return Result.Success(new BillingCheckoutSessionModel("new_checkout", "https://checkout.example/new", user.Id.Value.ToString(), "price", "monthly"));
        });
        IBillingProviderGatewayAccessor accessor = Substitute.For<IBillingProviderGatewayAccessor>();
        accessor.GetProviderOrDefault(BillingProviderNames.YooKassa).Returns(gateway);
        var handler = new CreateCheckoutSessionCommandHandler(users, subscriptions,
            provider.GetRequiredService<IBillingPaymentWriteRepository>(), accessor, TimeProvider.System,
            provider.GetRequiredService<IBillingCheckoutLock>(), runner);

        Result<BillingCheckoutSessionModel> result = await handler.Handle(new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.YooKassa), CancellationToken.None);

        if (webhookStatus is null) {
            Assert.True(result.IsSuccess);
            Assert.Equal(BillingSubscription.PendingCheckoutStatus, (await context.BillingSubscriptions.AsNoTracking().SingleAsync()).Status);
            Assert.Equal("new_checkout", (await context.BillingPayments.AsNoTracking().SingleAsync()).ExternalPaymentId);
            return;
        }
        Assert.True(result.IsFailure);
        Assert.Equal(string.Equals(webhookStatus, "active", StringComparison.Ordinal) ? "Billing.SubscriptionAlreadyActive" : "Billing.CheckoutAlreadyInProgress", result.Error.Code);
        BillingSubscription saved = await context.BillingSubscriptions.AsNoTracking().SingleAsync();
        Assert.Equal(webhookStatus, saved.Status);
        Assert.Equal("concurrent_webhook", saved.LastWebhookEventId);
        Assert.Equal("paid_old_session", saved.ExternalSubscriptionId);
        Assert.False(await context.BillingPayments.AsNoTracking().AnyAsync(payment => payment.ExternalPaymentId == "new_checkout"));
        await gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }
}
