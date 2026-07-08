using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Billing.Models;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {

    [Fact]
    public async Task BillingRenewalService_ForDueSubscription_UpdatesSubscriptionAddsPaymentAndKeepsPremiumRole() {
        User user = CreatePremiumUser("premium@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_renewal",
            "pay_initial",
            "pm_renewal",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_initial",
            Now.AddMonths(-1),
            "{\"initial\":true}");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            CreateRenewalPayment(
                "pay_renewed",
                "pm_renewal",
                "evt_renewed",
                "{\"renewed\":true}"));
        BillingRenewalService service = CreateRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            renewalGateway);

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Renewed);
        Assert.Equal(0, result.Failed);
        Assert.Equal("pay_renewed", subscription.ExternalSubscriptionId);
        Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc);
        Assert.Equal("{\"renewed\":true}", subscription.ProviderMetadataJson);

        BillingPayment payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Renewal, payment.Kind);
        Assert.Equal("pay_renewed", payment.ExternalPaymentId);
        Assert.Equal("pm_renewal", payment.ExternalPaymentMethodId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.True(user.HasRole(RoleNames.Premium));
    }


    [Fact]
    public async Task BillingRenewalService_WhenRenewalPaymentAlreadyExists_ReturnsRenewed() {
        User user = CreatePremiumUser("renewal-duplicate-payment@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_renewal_duplicate",
            "pay_initial_duplicate",
            "pm_renewal_duplicate",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_initial_duplicate",
            Now.AddMonths(-1));
        var paymentRepository = new RecordingBillingPaymentRepository {
            ThrowAlreadyExistsOnAdd = true,
        };
        BillingRenewalService service = CreateRenewalService(
            new InMemoryBillingSubscriptionRepository(subscription),
            paymentRepository,
            new FakeUserRepository(user),
            new FakeRecurringBillingGateway(
                BillingProviderNames.YooKassa,
                CreateRenewalPayment(
                    "pay_renewed_duplicate",
                    "pm_renewal_duplicate",
                    "evt_renewed_duplicate")));

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Renewed);
        Assert.Equal(0, result.Failed);
        Assert.Empty(paymentRepository.Payments);
    }


    [Fact]
    public async Task BillingRenewalService_WhenRenewalPaymentExists_SkipsAddingPayment() {
        User user = CreatePremiumUser("renewal-existing-payment@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_renewal_existing",
            "pay_initial_existing",
            "pm_renewal_existing",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_initial_existing",
            Now.AddMonths(-1));
        var paymentRepository = new RecordingBillingPaymentRepository();
        paymentRepository.Payments.Add(BillingPayment.Create(
            subscription.UserId,
            subscription.Id,
            BillingProviderNames.YooKassa,
            "pay_renewed_existing",
            subscription.ExternalCustomerId,
            "pay_initial_existing",
            "pm_renewal_existing",
            "price_monthly",
            "monthly",
            "active",
            BillingPaymentKinds.Renewal,
            7.99m,
            "USD",
            Now.AddMonths(-1),
            Now,
            "evt_existing_payment",
            providerMetadataJson: null));
        BillingRenewalService service = CreateRenewalService(
            new InMemoryBillingSubscriptionRepository(subscription),
            paymentRepository,
            new FakeUserRepository(user),
            new FakeRecurringBillingGateway(
                BillingProviderNames.YooKassa,
                CreateRenewalPayment(
                    "pay_renewed_existing",
                    "pm_renewal_existing",
                    "evt_renewed_existing")));

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Renewed);
        Assert.Single(paymentRepository.Payments);
    }


    [Fact]
    public async Task BillingRenewalService_WhenProviderMissing_DoesNotProcessSubscriptions() {
        var service = new BillingRenewalService(
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeUserRepository(),
            new NoOpBillingTransactionRunner(),
            [],
            new BillingAccessService(
                new FakeUserRepository(),
                new InMemoryBillingSubscriptionRepository(),
                new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(0, result.Failed);
    }


    [Fact]
    public async Task BillingRenewalService_WhenRenewalFails_MarksPastDueAndRemovesBillingManagedPremiumRole() {
        var premiumRole = Role.Create(RoleNames.Premium);
        var user = User.Create("failed-renewal@example.com", "hash");
        user.ReplaceRoles([premiumRole]);
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_failed",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_initial",
            "pm_failed",
            "price_monthly",
            "monthly",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            "evt_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var service = new BillingRenewalService(
            subscriptionRepository,
            new RecordingBillingPaymentRepository(),
            userRepository,
            new NoOpBillingTransactionRunner(),
            [new FailingRecurringBillingGateway(BillingProviderNames.YooKassa)],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(1, result.Failed);
        Assert.Equal("past_due", subscription.Status);
        Assert.Equal(Now.AddHours(1), subscription.NextBillingAttemptUtc);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.False(user.HasRole(RoleNames.Premium));
    }


    [Fact]
    public async Task BillingRenewalService_WhenBillingDetailsMissing_MarksPastDueWithoutCallingProvider() {
        User user = CreatePremiumUser("missing-renewal-details@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_missing_details",
            "pay_initial",
            externalPaymentMethodId: null,
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            CreateRenewalPayment(
                "pay_should_not_be_used",
                "pm_should_not_be_used",
                "evt_should_not_be_used"));
        BillingRenewalService service = CreateRenewalService(
            subscriptionRepository,
            new RecordingBillingPaymentRepository(),
            userRepository,
            renewalGateway);

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, renewalGateway.CreatePaymentCallCount);
        Assert.Equal("past_due", subscription.Status);
        Assert.Equal(Now.AddHours(1), subscription.NextBillingAttemptUtc);
        Assert.Equal("Renewal skipped because subscription billing details are incomplete.", subscription.ProviderMetadataJson);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.False(user.HasRole(RoleNames.Premium));
    }


    [Fact]
    public async Task BillingRenewalService_ForDeletedUserSubscription_SkipsProviderAndDisablesRenewal() {
        var user = User.Create("deleted-renewal@example.com", "hash");
        user.DeleteAccount(Now.AddDays(-1));
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_deleted_renewal",
            "pay_deleted_initial",
            "pm_deleted_renewal",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_deleted_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            CreateRenewalPayment(
                "pay_should_not_be_used",
                "pm_deleted_renewal",
                "evt_should_not_be_used"));
        BillingRenewalService service = CreateRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            renewalGateway);

        BillingRenewalRunResult result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, renewalGateway.CreatePaymentCallCount);
        Assert.Empty(paymentRepository.Payments);
        Assert.Equal("canceled", subscription.Status);
        Assert.Null(subscription.NextBillingAttemptUtc);
        Assert.Equal(Now, subscription.CanceledAtUtc);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(0, userRepository.UpdateCount);
    }


    [Fact]
    public async Task BillingAccessService_WhenOnlyManagedFlagChanges_PersistsSubscription() {
        var user = User.Create("managed-flag@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_managed_flag",
            "price_monthly",
            "monthly");
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddDays(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var service = new BillingAccessService(
            userRepository,
            subscriptionRepository,
            new FixedDateTimeProvider(Now));

        await service.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium: false, CancellationToken.None);

        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(1, subscriptionRepository.UpdateCount);
        Assert.Equal(0, userRepository.UpdateCount);
    }


    [Fact]
    public async Task BillingAccessService_WhenGrantingPremium_AddsRoleAndMarksSubscriptionManaged() {
        var user = User.Create("managed-premium-grant@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_managed_premium_grant",
            "price_monthly",
            "monthly");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var service = new BillingAccessService(
            userRepository,
            subscriptionRepository,
            new FixedDateTimeProvider(Now));

        await service.EnsurePremiumRoleAsync(user, subscription, shouldHavePremium: true, CancellationToken.None);

        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.True(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(1, userRepository.RoleMembershipWriteCount);
        Assert.Equal(0, userRepository.UpdateCount);
        Assert.Equal(1, subscriptionRepository.UpdateCount);
    }


    [Fact]
    public void BillingAccessService_WithBlankStatus_DoesNotGrantPremiumAccess() {
        var service = new BillingAccessService(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FixedDateTimeProvider(Now));

        bool shouldHavePremium = service.ShouldHavePremiumAccess(" ", Now.AddDays(1));

        Assert.False(shouldHavePremium);
    }


    [Theory]
    [InlineData("trialing", 1, true)]
    [InlineData("trialing", -1, false)]
    [InlineData("active", null, true)]
    [InlineData("past_due", null, true)]
    [InlineData("past_due", 1, true)]
    [InlineData("past_due", -1, false)]
    [InlineData("canceled", 1, false)]
    public void BillingAccessService_ShouldHavePremiumAccess_CoversStatusBranches(
        string status,
        int? periodEndOffsetDays,
        bool expected) {
        var service = new BillingAccessService(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FixedDateTimeProvider(Now));
        DateTime? periodEnd = periodEndOffsetDays.HasValue
            ? Now.AddDays(periodEndOffsetDays.Value)
            : (DateTime?)null;

        bool shouldHavePremium = service.ShouldHavePremiumAccess(status, periodEnd);

        Assert.Equal(expected, shouldHavePremium);
    }

}
