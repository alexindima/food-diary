using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task BillingWebhookPaymentRecorder_WithExistingPayment_UpdatesProviderResultWithoutAddingDuplicate() {
        var user = User.Create("payment-update@example.com", "hash");
        var repository = new RecordingBillingPaymentRepository();
        var recorder = new BillingWebhookPaymentRecorder(repository);
        BillingWebhookEventModel original = CreateWebhookPaymentEvent(user, "event-original", "subscription") with {
            ExternalPaymentId = "payment",
            OccurredAtUtc = Now,
        };
        await recorder.AddIfPresentAsync(subscription: null, user.Id, BillingProviderNames.YooKassa, original, CancellationToken.None);
        BillingPayment payment = Assert.Single(repository.Payments);
        BillingWebhookEventModel updated = original with {
            EventId = "event-updated",
            Amount = 5.99m,
            Status = "refunded",
            FinancialAction = BillingPaymentKinds.Refund,
            OccurredAtUtc = Now.AddMinutes(1),
        };

        await recorder.AddIfPresentAsync(subscription: null, user.Id, BillingProviderNames.YooKassa, updated, CancellationToken.None);

        Assert.Same(payment, Assert.Single(repository.Payments));
        Assert.Multiple(
            () => Assert.Equal(5.99m, payment.Amount),
            () => Assert.Equal("refunded", payment.Status),
            () => Assert.Equal(BillingPaymentKinds.Refund, payment.Kind),
            () => Assert.Equal("event-updated", payment.WebhookEventId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BillingAccessService_WithUnmanagedPremium_DoesNotChangeExternallyGrantedRole(bool shouldHavePremium) {
        User user = CreatePremiumUser("unmanaged-premium@example.com");
        var subscription = BillingSubscription.CreatePending(
            user.Id, BillingProviderNames.YooKassa, "customer", "price", "monthly");
        var users = new FakeUserRepository(user);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var service = new BillingAccessService(users, subscriptions, new FixedDateTimeProvider(Now));

        await service.EnsurePremiumRoleAsync(CreateBillingProfile(user), subscription, shouldHavePremium, CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(user.HasRole(RoleNames.Premium)),
            () => Assert.False(subscription.PremiumRoleManagedByBilling),
            () => Assert.Equal(0, users.RoleMembershipWriteCount),
            () => Assert.Equal(0, subscriptions.UpdateCount));
    }
}
