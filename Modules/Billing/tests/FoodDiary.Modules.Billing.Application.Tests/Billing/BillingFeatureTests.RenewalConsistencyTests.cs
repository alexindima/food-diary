using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData("renewed")]
    [InlineData("canceled")]
    [InlineData("provider-changed")]
    [InlineData("cancel-at-end")]
    public async Task RenewalBatch_RechecksEachSubscriptionBeforeCallingProvider(string change) {
        User first = CreatePremiumUser("batch-first@example.com");
        User second = CreatePremiumUser("batch-second@example.com");
        BillingSubscription firstSubscription = CreateSubscriptionSnapshot(first, BillingProviderNames.YooKassa,
            "customer_first", "pay_first", "pm_first", "active", Now.AddMonths(-1), Now.AddDays(-1), "initial_first", Now.AddMonths(-1));
        BillingSubscription secondSubscription = CreateSubscriptionSnapshot(second, BillingProviderNames.YooKassa,
            "customer_second", "pay_second", "pm_second", "active", Now.AddMonths(-1), Now.AddDays(-1), "initial_second", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(firstSubscription, secondSubscription);
        var payments = new RecordingBillingPaymentRepository();
        var gateway = new FakeRecurringBillingGateway(BillingProviderNames.YooKassa,
            CreateRenewalPayment("pay_first_new", "pm_first", "renewed_first"), () => {
                secondSubscription.ApplyProviderSnapshot(
                    string.Equals(change, "provider-changed", StringComparison.Ordinal) ? BillingProviderNames.Stripe : BillingProviderNames.YooKassa,
                    "pay_second_new", "pm_second", "price", "monthly", string.Equals(change, "canceled", StringComparison.Ordinal) ? "canceled" : "active",
                    Now.AddMonths(-1), string.Equals(change, "renewed", StringComparison.Ordinal) ? Now.AddMonths(2) : Now.AddDays(-1),
                    cancelAtPeriodEnd: string.Equals(change, "cancel-at-end", StringComparison.Ordinal), canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                    "second_updated", Now, webhookOccurredAtUtc: Now);
            });

        await CreateRenewalHandler(subscriptions, payments, new FakeUserRepository(first, second), gateway)
            .Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(1, gateway.CreatePaymentCallCount),
            () => Assert.Equal("second_updated", secondSubscription.LastWebhookEventId),
            () => Assert.Equal("pay_first_new", Assert.Single(payments.Payments).ExternalPaymentId));
    }

    [Theory]
    [InlineData(false, "active", false, false)]
    [InlineData(true, "active", false, false)]
    [InlineData(false, "active", true, false)]
    [InlineData(true, "active", true, false)]
    [InlineData(false, "canceled", false, false)]
    [InlineData(true, "canceled", false, false)]
    [InlineData(false, "active", false, true)]
    [InlineData(true, "active", false, true)]
    [InlineData(false, "canceled", false, true)]
    [InlineData(true, "canceled", false, true)]
    public async Task RenewalResponseAndWebhook_InEitherOrder_KeepPeriodAndRetryConsistent(bool webhookFirst, string status, bool hasAnchor, bool legacyQueued) {
        User user = CreatePremiumUser("consistent-renewal@example.com");
        DateTime oldEnd = Now.AddDays(-1);
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_consistent", "pay_initial", "pm_consistent", "active", oldEnd.AddMonths(-1), oldEnd, "initial", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        DateTime? webhookStart = hasAnchor ? oldEnd : null;
        DateTime? webhookEnd = hasAnchor ? oldEnd.AddMonths(1) : null;
        if (legacyQueued) {
            webhookStart = Now;
            webhookEnd = string.Equals(status, "active", StringComparison.Ordinal) ? Now.AddMonths(1) : null;
        }
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "webhook_final", "pay_final") with {
            ExternalCustomerId = "customer_consistent",
            ExternalPaymentMethodId = "pm_consistent",
            Status = status,
            CurrentPeriodStartUtc = webhookStart,
            CurrentPeriodEndUtc = webhookEnd,
            IsRenewal = !legacyQueued,
            OccurredAtUtc = Now,
            IsAuthoritativeSnapshot = true,
            ProviderMetadataJson = legacyQueued ? """{"metadata":{"renewal":"true","plan":"monthly"}}""" : null,
        };
        ProcessBillingWebhookCommandHandler webhookHandler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhook),
            users, subscriptions, payments, new RecordingBillingWebhookEventRepository());
        var webhookCommand = new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty);
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                if (webhookFirst) {
                    ResultAssert.Success(await webhookHandler.Handle(webhookCommand, CancellationToken.None));
                }
                return Result.Success(CreateRenewalPayment("pay_final", "pm_consistent", "response_final") with {
                    Status = status,
                    CurrentPeriodStartUtc = string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd : null,
                    CurrentPeriodEndUtc = string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd.AddMonths(1) : oldEnd,
                    OccurredAtUtc = Now,
                });
            });

        await CreateRenewalHandler(subscriptions, payments, users, gateway)
            .Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);
        if (!webhookFirst) {
            ResultAssert.Success(await webhookHandler.Handle(webhookCommand, CancellationToken.None));
        }

        BillingPayment payment = Assert.Single(payments.Payments);
        Assert.Multiple(
            () => Assert.Equal(string.Equals(status, "active", StringComparison.Ordinal) ? "active" : "past_due", subscription.Status),
            () => Assert.Equal(string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd.AddMonths(1) : oldEnd, subscription.CurrentPeriodEndUtc),
            () => Assert.Equal(string.Equals(status, "active", StringComparison.Ordinal) ? oldEnd.AddMonths(1) : Now.AddHours(1), subscription.NextBillingAttemptUtc),
            () => Assert.Equal(status, payment.Status),
            () => Assert.Equal(BillingPaymentKinds.Renewal, payment.Kind),
            () => Assert.Equal(Now, payment.OccurredAtUtc),
            () => Assert.Equal(Now, subscription.LastWebhookOccurredAtUtc));
        if (string.Equals(status, "active", StringComparison.Ordinal)) {
            Assert.Equal(oldEnd, payment.CurrentPeriodStartUtc);
            Assert.Equal(oldEnd.AddMonths(1), payment.CurrentPeriodEndUtc);
        } else {
            Assert.Null(payment.CurrentPeriodStartUtc);
        }
    }

    [Theory]
    [InlineData("active")]
    [InlineData("canceled")]
    public async Task PendingResponse_AcceptsFinalWebhookForSamePaymentAtSameOccurrence(string status) {
        User user = CreatePremiumUser("pending-final@example.com");
        DateTime oldEnd = Now.AddDays(-1);
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_pending_final", "pay_initial", "pm_pending_final", "active", oldEnd.AddMonths(-1), oldEnd, "initial", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        var gateway = new FakeRecurringBillingGateway(BillingProviderNames.YooKassa,
            CreateRenewalPayment("pay_pending_final", "pm_pending_final", "pending_response") with {
                Status = "pending",
                CurrentPeriodStartUtc = null,
                CurrentPeriodEndUtc = oldEnd,
                OccurredAtUtc = Now,
            });
        await CreateRenewalHandler(subscriptions, payments, users, gateway)
            .Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "final_webhook", "pay_pending_final") with {
            ExternalCustomerId = "customer_pending_final",
            ExternalPaymentMethodId = "pm_pending_final",
            Status = status,
            CurrentPeriodStartUtc = null,
            CurrentPeriodEndUtc = null,
            IsRenewal = true,
            OccurredAtUtc = Now,
            IsAuthoritativeSnapshot = true,
        };
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhook),
            users, subscriptions, payments, new RecordingBillingWebhookEventRepository());

        ResultAssert.Success(await handler.Handle(new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty), CancellationToken.None));

        bool paid = string.Equals(status, "active", StringComparison.Ordinal);
        Assert.Equal(status, Assert.Single(payments.Payments).Status);
        Assert.Equal(paid ? "active" : "past_due", subscription.Status);
        Assert.Equal(paid ? oldEnd.AddMonths(1) : Now.AddHours(1), subscription.NextBillingAttemptUtc);
    }

    [Theory]
    [InlineData(-1, "canceled")]
    [InlineData(0, "canceled")]
    [InlineData(-1, "active")]
    [InlineData(0, "active")]
    public async Task SuccessfulRenewal_RejectsOlderOrTiedOtherPaymentUpdate(int daysBefore, string oldStatus) {
        User user = CreatePremiumUser("renewal-cursor@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_cursor", "pay_initial", "pm_cursor", "active", Now.AddMonths(-1), Now.AddDays(-1), "initial", Now.AddMonths(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        var gateway = new FakeRecurringBillingGateway(BillingProviderNames.YooKassa,
            CreateRenewalPayment("pay_new", "pm_cursor", "response_new") with { OccurredAtUtc = Now });
        await CreateRenewalHandler(subscriptions, payments, users, gateway)
            .Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);
        BillingWebhookEventModel oldWebhook = CreateWebhookPaymentEvent(user, "webhook_old", "pay_old") with {
            ExternalCustomerId = "customer_cursor",
            ExternalPaymentMethodId = "pm_cursor",
            Status = oldStatus,
            CurrentPeriodStartUtc = null,
            CurrentPeriodEndUtc = null,
            OccurredAtUtc = Now.AddDays(daysBefore),
            IsAuthoritativeSnapshot = true,
            IsRenewal = true,
        };
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: oldWebhook),
            users, subscriptions, payments, new RecordingBillingWebhookEventRepository());

        ResultAssert.Success(await handler.Handle(new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty), CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal("active", subscription.Status),
            () => Assert.Equal("response_new", subscription.LastWebhookEventId),
            () => Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc),
            () => Assert.Equal(2, payments.Payments.Count));
        if (string.Equals(oldStatus, "active", StringComparison.Ordinal)) {
            Assert.Equal(Now.AddDays(daysBefore), payments.Payments.Single(payment => string.Equals(payment.ExternalPaymentId, "pay_old", StringComparison.Ordinal)).CurrentPeriodStartUtc);
        }
    }

    [Theory]
    [InlineData(BillingProviderNames.YooKassa, BillingProviderNames.Stripe)]
    [InlineData(BillingProviderNames.YooKassa, BillingProviderNames.Paddle)]
    [InlineData(BillingProviderNames.Stripe, BillingProviderNames.Stripe)]
    public async Task Checkout_ReusesCustomerOnlyForTheSameProvider(string oldProvider, string newProvider) {
        var user = User.Create("provider-switch@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, oldProvider,
            "old_customer", "old_subscription", "old_method", "canceled", Now.AddMonths(-1), Now.AddDays(-1), "old_event", Now.AddDays(-1));
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        var gateway = new FakeBillingProviderGateway(newProvider,
            checkoutSession: new BillingCheckoutSessionModel("checkout_new", "https://checkout.example/new", "new_customer", "price", "monthly"));
        var handler = new CreateCheckoutSessionCommandHandler(users, subscriptions, payments,
            new FakeBillingProviderGatewayAccessor(gateway), new FixedDateTimeProvider(Now), new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());

        ResultAssert.Success(await handler.Handle(new CreateCheckoutSessionCommand(user.Id.Value, "monthly", newProvider), CancellationToken.None));

        Assert.Equal(string.Equals(oldProvider, newProvider, StringComparison.Ordinal) ? "old_customer" : null, gateway.LastCheckoutRequest?.ExistingCustomerId);
        Assert.Null(subscription.NextBillingAttemptUtc);
        if (!string.Equals(oldProvider, newProvider, StringComparison.Ordinal)) {
            Assert.Null(subscription.ExternalPaymentMethodId);
            Assert.Null(subscription.ExternalSubscriptionId);
            Assert.Null(subscription.LastWebhookOccurredAtUtc);
        }
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, "new_event", "new_subscription") with {
            ExternalCustomerId = "new_customer",
            ExternalPaymentMethodId = "new_method",
            OccurredAtUtc = Now,
        };
        ProcessBillingWebhookCommandHandler webhookHandler = CreateWebhookHandler(
            new FakeBillingProviderGateway(newProvider, webhookEvent: webhook), users, subscriptions, payments, new RecordingBillingWebhookEventRepository());
        ResultAssert.Success(await webhookHandler.Handle(new ProcessBillingWebhookCommand(newProvider, "{}", string.Empty), CancellationToken.None));
        Assert.Equal("active", subscription.Status);
        Assert.Equal("new_subscription", subscription.ExternalSubscriptionId);
        if (!string.Equals(oldProvider, newProvider, StringComparison.Ordinal)) {
            ProcessBillingWebhookCommandHandler oldHandler = CreateWebhookHandler(
                new FakeBillingProviderGateway(oldProvider, webhookEvent: webhook with {
                    EventId = "late_old",
                    ExternalCustomerId = "old_customer",
                    ExternalSubscriptionId = "old_subscription",
                    ExternalPaymentMethodId = "old_method",
                    Status = "canceled",
                    OccurredAtUtc = Now.AddMinutes(1),
                }), users, subscriptions, payments, new RecordingBillingWebhookEventRepository());
            ResultAssert.Success(await oldHandler.Handle(new ProcessBillingWebhookCommand(oldProvider, "{}", string.Empty), CancellationToken.None));
            Assert.Equal(newProvider, subscription.Provider);
            Assert.Equal("active", subscription.Status);
            Assert.Single(subscriptions.Subscriptions);
        }
    }
}
