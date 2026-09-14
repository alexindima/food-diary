using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData("matching")]
    [InlineData("user")]
    [InlineData("subscription")]
    [InlineData("customer")]
    [InlineData("price")]
    [InlineData("plan")]
    [InlineData("status")]
    [InlineData("kind")]
    public async Task Checkout_WhenProviderReplaysSavedSession_ValidatesOwnershipAndContext(string scenario) {
        var user = User.Create("checkout-replay@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.Stripe, "cus_123", "price_monthly", "monthly");
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var saved = BillingPayment.Create(
            string.Equals(scenario, "user", StringComparison.Ordinal) ? User.Create("other@example.com", "hash").Id : user.Id,
            string.Equals(scenario, "subscription", StringComparison.Ordinal) ? Guid.NewGuid() : subscription.Id,
            BillingProviderNames.Stripe, "cs_saved", string.Equals(scenario, "customer", StringComparison.Ordinal) ? "other" : "cus_123",
            externalSubscriptionId: null, externalPaymentMethodId: null,
            string.Equals(scenario, "price", StringComparison.Ordinal) ? "other" : "price_monthly", string.Equals(scenario, "plan", StringComparison.Ordinal) ? "yearly" : "monthly",
            string.Equals(scenario, "status", StringComparison.Ordinal) ? "completed" : BillingSubscription.PendingCheckoutStatus,
            string.Equals(scenario, "kind", StringComparison.Ordinal) ? BillingPaymentKinds.Transaction : BillingPaymentKinds.Checkout,
            amount: null, currency: null, currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: null, providerMetadataJson: null);
        await payments.AddAsync(saved, CancellationToken.None);
        DateTime? modified = subscription.ModifiedOnUtc;
        var session = new BillingCheckoutSessionModel("cs_saved", "https://checkout.example/saved", "cus_123", "price_monthly", "monthly");
        var gateway = new FakeBillingProviderGateway(BillingProviderNames.Stripe, checkoutSession: session);
        var handler = new CreateCheckoutSessionCommandHandler(new FakeUserRepository(user), subscriptions, payments,
            new FakeBillingProviderGatewayAccessor(gateway), new FixedDateTimeProvider(DateTime.UtcNow.AddMinutes(30)),
            new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.Stripe, "same_key"), CancellationToken.None);

        Assert.Equal(string.Equals(scenario, "matching", StringComparison.Ordinal), result.IsSuccess);
        if (result.IsSuccess) {
            Assert.Equal(session, result.Value);
        } else {
            Assert.Equal("Billing.CheckoutAlreadyInProgress", result.Error.Code);
        }
        Assert.Same(saved, Assert.Single(payments.Payments));
        Assert.Equal(modified, subscription.ModifiedOnUtc);
        Assert.Equal(0, subscriptions.UpdateCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Checkout_WhenWebhookActivatesDuringProviderCall_PreservesSubscription(bool existingCheckout) {
        var user = User.Create("checkout-webhook@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        var subscriptions = new InMemoryBillingSubscriptionRepository();
        if (existingCheckout) {
            await subscriptions.AddAsync(BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa,
                "customer_existing_webhook_payment", "price_monthly", "monthly"), CancellationToken.None);
        }
        var users = new FakeUserRepository(user);
        var payments = new RecordingBillingPaymentRepository();
        BillingWebhookEventModel model = CreateWebhookPaymentEvent(user, "late_success", "old_payment") with { OccurredAtUtc = Now };
        var webhookGateway = new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: model);
        ProcessBillingWebhookCommandHandler webhook = CreateWebhookHandler(webhookGateway, users, subscriptions, payments,
            new RecordingBillingWebhookEventRepository());
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                ResultAssert.Success(await webhook.Handle(new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", ""), CancellationToken.None));
                return Result.Success(new BillingCheckoutSessionModel("new_session", "https://checkout.example/new", "customer_new", "price_monthly", "monthly"));
            });
        var runner = new NoOpBillingTransactionRunner();
        var handler = new CreateCheckoutSessionCommandHandler(users, subscriptions, payments,
            new FakeBillingProviderGatewayAccessor(gateway), new FixedDateTimeProvider(DateTime.UtcNow.AddMinutes(30)),
            new NoopBillingCheckoutLock(), runner);

        Result<BillingCheckoutSessionModel> result = await handler.Handle(new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.YooKassa), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.SubscriptionAlreadyActive", result.Error.Code);
        BillingSubscription subscription = Assert.Single(subscriptions.Subscriptions);
        Assert.Equal("active", subscription.Status);
        Assert.Equal("old_payment", subscription.ExternalSubscriptionId);
        Assert.Equal("late_success", subscription.LastWebhookEventId);
        Assert.Equal(Now.AddMonths(1), subscription.NextBillingAttemptUtc);
        Assert.DoesNotContain(payments.Payments, payment => string.Equals(payment.ExternalPaymentId, "new_session", StringComparison.Ordinal));
        Assert.Equal($"billing-user:{user.Id.Value:N}", runner.LastSerializationKey);
    }

    [Fact]
    public async Task StripeInvoices_WithDuplicateDeliveryAndLaterInvoice_RecordEachInvoiceWithoutChangingSubscription() {
        User user = CreatePremiumUser("stripe-ledger@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.Stripe, "cus_123", "sub_123",
            "pm_123", "active", Now, Now.AddMonths(1), "current_subscription", Now);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        foreach ((string eventId, string invoiceId) in new[] { ("evt_paid", "in_1"), ("evt_succeeded", "in_1"), ("evt_next", "in_2") }) {
            BillingWebhookEventModel model = CreateWebhookPaymentEvent(user, eventId, "sub_123") with {
                EventType = "invoice.paid",
                ExternalCustomerId = "cus_123",
                ExternalPaymentId = invoiceId,
                Status = "completed",
                Amount = 7.991m,
                Currency = "BHD",
                UpdatesSubscription = false,
                OccurredAtUtc = Now.AddDays(-2),
                CurrentPeriodStartUtc = Now.AddMonths(-1),
                CurrentPeriodEndUtc = Now,
            };
            var gateway = new FakeBillingProviderGateway(BillingProviderNames.Stripe, webhookEvent: model);
            ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(gateway, users, subscriptions, payments,
                new RecordingBillingWebhookEventRepository());
            ResultAssert.Success(await handler.Handle(new ProcessBillingWebhookCommand(BillingProviderNames.Stripe, "{}", ""), CancellationToken.None));
        }

        Assert.Equal(2, payments.Payments.Count);
        Assert.All(payments.Payments, payment => {
            Assert.Equal(BillingPaymentKinds.Transaction, payment.Kind);
            Assert.Equal("completed", payment.Status);
            Assert.Equal(7.991m, payment.Amount);
            Assert.Equal(Now.AddMonths(-1), payment.CurrentPeriodStartUtc);
        });
        Assert.Equal("current_subscription", subscription.LastWebhookEventId);
        Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc);
        Assert.Equal(0, users.RoleMembershipWriteCount);
    }
}
