using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Contracts.Commands.RenewDueSubscriptions;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    public async Task Webhook_WhenAccountIsInaccessible_PreservesOnlyValidOrManualPremium(
        bool deleted, bool managed, bool ended) {
        User user = CreatePremiumUser("lifecycle-webhook@example.com");
        MakeInaccessible(user, deleted);
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "lifecycle_customer", "lifecycle_payment", "lifecycle_method", ended ? "canceled" : "active",
            Now.AddMonths(-1), ended ? Now : Now.AddMonths(1), "lifecycle_event", Now);
        subscription.MarkPremiumRoleManagedByBilling(managed, Now);
        var users = new FakeUserRepository(user);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var syncer = new BillingWebhookPremiumRoleSyncer(
            new BillingAccessService(users, subscriptions, new FixedDateTimeProvider(Now)),
            new NoOpMarketingConversionRecorder());

        await syncer.SyncAsync(CreateBillingProfile(user), subscription,
            CreateWebhookPaymentEvent(user, "lifecycle_event", "lifecycle_payment"), CancellationToken.None);
        await syncer.SyncAsync(CreateBillingProfile(user), subscription,
            CreateWebhookPaymentEvent(user, "lifecycle_event", "lifecycle_payment"), CancellationToken.None);

        RestoreAccessibility(user, deleted);
        Assert.Multiple(
            () => Assert.Equal(!managed || !ended, user.HasRole(RoleNames.Premium)),
            () => Assert.Equal(managed && !ended, subscription.PremiumRoleManagedByBilling),
            () => Assert.Equal(managed && ended ? 1 : 0, users.RoleMembershipWriteCount));
    }

    [Theory]
    [InlineData(true, true, "before-provider")]
    [InlineData(true, false, "before-provider")]
    [InlineData(false, true, "before-provider")]
    [InlineData(false, false, "before-provider")]
    [InlineData(true, true, "missing-details")]
    [InlineData(true, false, "missing-details")]
    [InlineData(false, true, "missing-details")]
    [InlineData(false, false, "missing-details")]
    [InlineData(true, true, "during-success")]
    [InlineData(true, false, "during-success")]
    [InlineData(false, true, "during-success")]
    [InlineData(false, false, "during-success")]
    [InlineData(true, true, "during-failure")]
    [InlineData(true, false, "during-failure")]
    [InlineData(false, true, "during-failure")]
    [InlineData(false, false, "during-failure")]
    public async Task Renewal_WhenAccountBecomesInaccessible_RevokesOnlyBillingOwnedPremium(
        bool deleted, bool managed, string transition) {
        User user = CreatePremiumUser("lifecycle-renewal@example.com");
        bool duringPayment = transition.StartsWith("during-", StringComparison.Ordinal);
        if (!duringPayment) {
            MakeInaccessible(user, deleted);
        }
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.YooKassa,
            "lifecycle_customer", "lifecycle_previous", transition is "missing-details" ? null : "lifecycle_method",
            "active", Now.AddMonths(-1), Now.AddMinutes(-1), "lifecycle_initial", Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(managed, Now.AddMonths(-1));
        var users = new FakeUserRepository(user);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        IBillingRecurringProviderGateway gateway = Substitute.For<IBillingRecurringProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.YooKassa);
        gateway.CreateRecurringPaymentAsync(Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                MakeInaccessible(user, deleted);
                return transition is "during-failure"
                    ? Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.YooKassa, "declined"))
                    : Result.Success(CreateRenewalPayment("lifecycle_renewed", "lifecycle_method", "lifecycle_renewed_event") with { OccurredAtUtc = Now });
            });
        var payments = new RecordingBillingPaymentRepository();
        RenewDueSubscriptionsCommandHandler handler = CreateRenewalHandler(subscriptions, payments, users, gateway);

        await handler.Handle(new RenewDueSubscriptionsCommand(BillingProviderNames.YooKassa, 10), CancellationToken.None);

        RestoreAccessibility(user, deleted);
        Assert.Multiple(
            () => Assert.Equal(!managed, user.HasRole(RoleNames.Premium)),
            () => Assert.False(subscription.PremiumRoleManagedByBilling),
            () => Assert.Equal(managed ? 1 : 0, users.RoleMembershipWriteCount));
        await gateway.Received(duringPayment ? 1 : 0).CreateRecurringPaymentAsync(
            Arg.Any<BillingRecurringPaymentRequestModel>(), Arg.Any<CancellationToken>());
        if (transition is "during-success") {
            Assert.Single(payments.Payments);
        }
    }

    private static void MakeInaccessible(User user, bool deleted) {
        if (deleted) {
            user.DeleteAccount(Now);
        } else {
            user.Deactivate(Now);
        }
    }

    private static void RestoreAccessibility(User user, bool deleted) {
        if (deleted) {
            user.Restore(Now.AddMinutes(1));
        } else {
            user.Activate(Now.AddMinutes(1));
        }
    }
}
