using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task ReplayedRenewalReusesAlreadyAppliedSubscriptionPeriodAsync() {
        var user = User.Create("renewal-replayed@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer", "pay_applied", "method", "active", Now, Now.AddMonths(1), "applied", Now);
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "replayed", "pay_applied") with {
            ExternalCustomerId = "customer",
            Status = "active",
            IsRenewal = true,
            CurrentPeriodStartUtc = Now.AddDays(5),
            CurrentPeriodEndUtc = Now.AddMonths(2),
            OccurredAtUtc = Now.AddMinutes(1),
        };
        var resolver = new BillingWebhookContextResolver(new InMemoryBillingSubscriptionRepository(subscription), new FakeUserRepository(user));
        Result<BillingWebhookProcessingContext?> result = await resolver.ResolveAsync(BillingProviderNames.YooKassa, webhook, CancellationToken.None);
        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.Equal(Now, result.Value.EffectiveEvent.CurrentPeriodStartUtc);
        Assert.Equal(Now.AddMonths(1), result.Value.EffectiveEvent.CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task InaccessibleUserRenewalDoesNotOverwriteConcurrentProviderChangeAsync() {
        var user = User.Create("renewal-inaccessible-race@example.com", "hash");
        user.Deactivate();
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_old", "pay_old", "method_old", "active", Now.AddMonths(-1), Now.AddDays(-1), "initial", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        IBillingTransactionRunner runner = Substitute.For<IBillingTransactionRunner>();
        int transactions = 0;
        runner.ExecuteSerializedAsync(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>()).Returns(async call => {
            if (++transactions == 2) { subscription.UpdateCheckoutContext(BillingProviderNames.Stripe, "customer_new", "price_new", "yearly"); }
            await call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>());
        });
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        await CreateRenewalHandler(subscriptions, new RecordingBillingPaymentRepository(), new FakeUserRepository(user), gateway, runner)
            .Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);
        Assert.Equal(2, transactions);
        Assert.Equal(BillingProviderNames.Stripe, subscription.Provider);
        Assert.Equal("customer_new", subscription.ExternalCustomerId);
        Assert.Equal(0, subscriptions.UpdateCount);
        await gateway.DidNotReceive().CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("[]")]
    [InlineData("{\"metadata\":{\"renewal\":false}}")]
    [InlineData("{\"metadata\":{\"renewal\":\"false\"}}")]
    public async Task InvalidLegacyMetadataDoesNotConvertCheckoutToRenewalAsync(string metadata) {
        var user = User.Create("legacy-invalid@example.com", "hash");
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "evt_legacy", "pay_legacy") with { ProviderMetadataJson = metadata };
        var resolver = new BillingWebhookContextResolver(new InMemoryBillingSubscriptionRepository(), new FakeUserRepository(user));
        Result<BillingWebhookProcessingContext?> result = await resolver.ResolveAsync(BillingProviderNames.YooKassa, webhook, CancellationToken.None);
        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.EffectiveEvent.IsRenewal);
        Assert.Equal(webhook.CurrentPeriodStartUtc, result.Value.EffectiveEvent.CurrentPeriodStartUtc);
    }

    [Theory]
    [InlineData("monthly")]
    [InlineData("yearly")]
    [InlineData("unknown")]
    public async Task LegacyRenewalWithoutSubscriptionUsesOccurrenceAsPeriodAnchorAsync(string plan) {
        var user = User.Create("legacy-anchor@example.com", "hash");
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "evt_anchor", "pay_anchor") with {
            Plan = plan,
            Status = "active",
            OccurredAtUtc = Now,
            CurrentPeriodStartUtc = null,
            CurrentPeriodEndUtc = null,
            ProviderMetadataJson = """{"metadata":{"renewal":"true"}}""",
        };
        var resolver = new BillingWebhookContextResolver(new InMemoryBillingSubscriptionRepository(), new FakeUserRepository(user));
        Result<BillingWebhookProcessingContext?> result = await resolver.ResolveAsync(BillingProviderNames.YooKassa, webhook, CancellationToken.None);
        ResultAssert.Success(result);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.EffectiveEvent.IsRenewal);
        Assert.Equal(Now, result.Value.EffectiveEvent.CurrentPeriodStartUtc);
        Assert.Equal(plan switch { "monthly" => Now.AddMonths(1), "yearly" => Now.AddYears(1), _ => (DateTime?)null }, result.Value.EffectiveEvent.CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task CheckoutRechecksUserAfterProviderReturnsAsync() {
        var user = User.Create("checkout-deactivated@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        var subscriptions = new InMemoryBillingSubscriptionRepository();
        var payments = new RecordingBillingPaymentRepository();
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.Stripe);
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>()).Returns(_ => {
            user.Deactivate();
            return Result.Success(new BillingCheckoutSessionModel("session", "https://checkout.example/session", "customer", "price_monthly", "monthly"));
        });
        var handler = new CreateCheckoutSessionCommandHandler(new FakeUserRepository(user), subscriptions, payments,
            new FakeBillingProviderGatewayAccessor(gateway), new FixedDateTimeProvider(Now), new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());
        Result<BillingCheckoutSessionModel> result = await handler.Handle(new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.Stripe), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Empty(payments.Payments);
        Assert.Null(await subscriptions.GetByUserIdAsync(user.Id));
    }
}
