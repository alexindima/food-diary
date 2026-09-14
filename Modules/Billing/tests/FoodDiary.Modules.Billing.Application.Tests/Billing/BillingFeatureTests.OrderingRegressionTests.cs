using FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task ProcessBillingWebhook_WhenPaymentAlreadyHasNewerState_DoesNotOverwriteIt() {
        User user = CreatePremiumUser("payment-state@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_state", "pay_state", "pm_state", "active", Now, Now.AddMonths(1), "evt_current", Now);
        var payments = new RecordingBillingPaymentRepository();
        var recorder = new BillingWebhookPaymentRecorder(payments);
        BillingWebhookEventModel current = CreateWebhookPaymentEvent(user, "evt_current", "pay_state") with {
            OccurredAtUtc = Now,
        };
        await recorder.AddIfPresentAsync(subscription, user.Id, BillingProviderNames.YooKassa, current, CancellationToken.None);

        await recorder.AddIfPresentAsync(subscription, user.Id, BillingProviderNames.YooKassa,
            current with { EventId = "evt_older", Status = "pending", OccurredAtUtc = Now.AddMinutes(-1) }, CancellationToken.None);

        BillingPayment payment = Assert.Single(payments.Payments);
        Assert.Multiple(
            () => Assert.Equal("active", payment.Status),
            () => Assert.Equal("evt_current", payment.WebhookEventId),
            () => Assert.Equal(Now, payment.OccurredAtUtc));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessBillingWebhook_WhenPaymentPredatesSubscription_RecordsPaymentWithoutChangingAccess(bool sameEventId) {
        User user = CreatePremiumUser("late-payment@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_late", "pay_new", "pm_late", "active", Now, Now.AddMonths(1), "evt_new", Now);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var payments = new RecordingBillingPaymentRepository();
        var users = new FakeUserRepository(user);
        BillingWebhookEventModel webhook = CreateWebhookPaymentEvent(user, sameEventId ? "evt_new" : "evt_old", "pay_old") with {
            ExternalCustomerId = "customer_late",
            ExternalPaymentMethodId = "pm_late",
            Status = "canceled",
            OccurredAtUtc = Now.AddDays(-1),
            IsAuthoritativeSnapshot = true,
        };
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhook),
            users, subscriptions, payments, new RecordingBillingWebhookEventRepository());

        var command = new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty);
        ResultAssert.Success(await handler.Handle(command, CancellationToken.None));
        ResultAssert.Success(await handler.Handle(command, CancellationToken.None));

        BillingPayment payment = Assert.Single(payments.Payments);
        Assert.Multiple(
            () => Assert.Equal("pay_old", payment.ExternalPaymentId),
            () => Assert.Equal(subscription.Id, payment.BillingSubscriptionId),
            () => Assert.Equal(7.99m, payment.Amount),
            () => Assert.Equal("canceled", payment.Status),
            () => Assert.Equal("active", subscription.Status),
            () => Assert.Equal("evt_new", subscription.LastWebhookEventId),
            () => Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc),
            () => Assert.Equal(0, subscriptions.UpdateCount),
            () => Assert.Equal(0, users.RoleMembershipWriteCount),
            () => Assert.True(user.HasRole(RoleNames.Premium)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenewDueSubscriptionsCommandHandler_WhenProfileChangesDuringPayment_UsesFreshProfile(bool deleted) {
        User user = CreatePremiumUser("changed-renewal@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_changed", "pay_initial", "pm_changed", "active", Now.AddMonths(-1), Now.AddMinutes(-1),
            "evt_initial", Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddMonths(-1));
        var users = new FakeUserRepository(user);
        var payments = new RecordingBillingPaymentRepository();
        var gateway = new FakeRecurringBillingGateway(BillingProviderNames.YooKassa,
            CreateRenewalPayment("pay_changed", "pm_changed", "evt_changed"), () => {
                user.ReplaceRoles([]);
                if (deleted) {
                    user.DeleteAccount(Now);
                }
            });
        RenewDueSubscriptionsCommandHandler service = CreateRenewalHandler(new InMemoryBillingSubscriptionRepository(subscription), payments, users, gateway);

        await service.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        Assert.Equal("pay_changed", Assert.Single(payments.Payments).ExternalPaymentId);
        Assert.Multiple(
            () => Assert.Equal(1, gateway.CreatePaymentCallCount),
            () => Assert.Equal(!deleted, user.HasRole(RoleNames.Premium)),
            () => Assert.Equal(!deleted, subscription.PremiumRoleManagedByBilling),
            () => Assert.Equal(deleted ? 0 : 1, users.RoleMembershipWriteCount),
            () => Assert.Equal(deleted ? "canceled" : "active", subscription.Status));
        if (deleted) {
            Assert.Null(subscription.NextBillingAttemptUtc);
        }
    }

    [Fact]
    public async Task RenewDueSubscriptionsCommandHandler_WhenTransactionIsReplayed_ReloadsProfileWithoutRepeatingPayment() {
        User user = CreatePremiumUser("replayed-renewal@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "customer_replay", "pay_initial", "pm_replay", "active", Now.AddMonths(-1), Now.AddMinutes(-1),
            "evt_initial", Now.AddMonths(-1));
        var users = new FakeUserRepository(user);
        var payments = new RecordingBillingPaymentRepository();
        var gateway = new FakeRecurringBillingGateway(BillingProviderNames.YooKassa,
            CreateRenewalPayment("pay_replay", "pm_replay", "evt_replay"));
        RenewDueSubscriptionsCommandHandler service = CreateRenewalHandler(new InMemoryBillingSubscriptionRepository(subscription),
            payments, users, gateway, new ReplayingBillingTransactionRunner(() => {
                if (payments.Payments.Count == 0) {
                    return; // The eligibility preflight has no business writes to roll back.
                }
                // Model rollback before retry; independently changed user roles must be reloaded.
                subscription.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "pay_initial", "pm_replay", "price_monthly",
                    "monthly", "active", Now.AddMonths(-1), Now.AddMinutes(-1), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                    "evt_initial", Now.AddMonths(-1), providerMetadataJson: null, Now.AddMonths(-1));
                payments.Payments.Clear();
                user.ReplaceRoles([]);
            }));

        await service.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        Assert.Single(payments.Payments);
        Assert.Multiple(
            () => Assert.Equal(1, gateway.CreatePaymentCallCount),
            () => Assert.Equal(1, users.RoleMembershipWriteCount),
            () => Assert.True(user.HasRole(RoleNames.Premium)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReplayingBillingTransactionRunner(Action betweenAttempts) : IBillingTransactionRunner {
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) {
            await operation(cancellationToken);
            betweenAttempts();
            await operation(cancellationToken);
        }

        public Task ExecuteSerializedAsync(string serializationKey, Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default) => ExecuteAsync(operation, cancellationToken);
    }
}
