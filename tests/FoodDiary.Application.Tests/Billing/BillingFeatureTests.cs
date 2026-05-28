using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Commands.StartPremiumTrial;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.TestHelper;

namespace FoodDiary.Application.Tests.Billing;

public sealed class BillingFeatureTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCheckoutSessionValidator_WithPaddedPlanAndProvider_ReturnsSuccess() {
        var result = await new CreateCheckoutSessionCommandValidator().TestValidateAsync(
            new CreateCheckoutSessionCommand(Guid.NewGuid(), " Monthly ", $" {BillingProviderNames.YooKassa} "));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateCheckoutSession_WithRequestedProvider_CreatesPendingSubscriptionAndCheckoutPayment() {
        var user = User.Create("buyer@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var yooKassaGateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            checkoutSession: new BillingCheckoutSessionModel(
                "pay_123",
                "https://checkout.example/pay_123",
                "customer_123",
                "price_monthly",
                "monthly"));
        var accessor = new FakeBillingProviderGatewayAccessor(
            activeProvider: new FakeBillingProviderGateway(BillingProviderNames.Paddle),
            yooKassaGateway);
        var handler = new CreateCheckoutSessionCommandHandler(
            userRepository,
            subscriptionRepository,
            paymentRepository,
            accessor,
            new FixedDateTimeProvider(Now));

        var result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, " Monthly ", $" {BillingProviderNames.YooKassa} "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example/pay_123", result.Value.Url);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal(BillingProviderNames.YooKassa, subscription.Provider);
        Assert.Equal("customer_123", subscription.ExternalCustomerId);
        Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Checkout, payment.Kind);
        Assert.Equal("pay_123", payment.ExternalPaymentId);
        Assert.Equal(subscription.Id, payment.BillingSubscriptionId);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForNewEvent_StoresSubscriptionPaymentWebhookAndAddsPremiumRole() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var providerMetadataJson = "{\"payment_id\":\"pay_456\"}";
        var webhookModel = new BillingWebhookEventModel(
            "evt_1",
            "payment.succeeded",
            "customer_456",
            "pay_456",
            "pm_456",
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            false,
            null,
            null,
            null,
            7.99m,
            "USD",
            providerMetadataJson,
            user.Id.Value);
        var gateway = new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhookModel);
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{\"event\":\"payment.succeeded\"}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal("active", subscription.Status);
        Assert.Equal("pay_456", subscription.ExternalSubscriptionId);
        Assert.Equal("pm_456", subscription.ExternalPaymentMethodId);
        Assert.Equal(providerMetadataJson, subscription.ProviderMetadataJson);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Webhook, payment.Kind);
        Assert.Equal("pay_456", payment.ExternalPaymentId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal("evt_1", payment.WebhookEventId);

        var webhookEvent = Assert.Single(webhookEventRepository.Events);
        Assert.Equal("evt_1", webhookEvent.EventId);
        Assert.Equal("processed", webhookEvent.Status);
        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.Equal(1, userRepository.UpdateCount);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForDuplicateEvent_ReturnsSuccessWithoutMutation() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        webhookEventRepository.ProcessedEventIds.Add("evt_duplicate");
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                "evt_duplicate",
                "payment.succeeded",
                "customer_456",
                "pay_456",
                "pm_456",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
                user.Id.Value));
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, userRepository.UpdateCount);
    }

    [Fact]
    public async Task ProcessBillingWebhook_WhenConcurrentDuplicateInsertDetected_ReturnsSuccessWithoutMutation() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository {
            ThrowAlreadyProcessedOnAdd = true
        };
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                "evt_race",
                "payment.succeeded",
                "customer_race",
                "pay_race",
                "pm_race",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
                user.Id.Value));
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, userRepository.UpdateCount);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForManualPremiumUser_DoesNotRemovePremiumRoleOnCanceledSubscription() {
        var premiumRole = Role.Create(RoleNames.Premium);
        var user = User.Create("manual-premium@example.com", "hash");
        user.ReplaceRoles([premiumRole]);
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_manual",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Paddle,
            "sub_manual",
            null,
            "price_monthly",
            "monthly",
            "active",
            Now.AddDays(-1),
            Now.AddMonths(1),
            false,
            null,
            null,
            null,
            "evt_initial",
            Now.AddDays(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.Paddle,
            webhookEvent: new BillingWebhookEventModel(
                "evt_canceled",
                "subscription.canceled",
                "customer_manual",
                "sub_manual",
                null,
                "price_monthly",
                "monthly",
                "canceled",
                Now.AddDays(-1),
                Now,
                false,
                Now,
                null,
                null,
                null,
                null,
                null,
                user.Id.Value));
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.Paddle, "{}", "signature"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.False(subscription.PremiumRoleManagedByBilling);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForDeletedUserSubscription_StoresEventWithoutGrantingPremiumRole() {
        var user = User.Create("deleted-premium@example.com", "hash");
        user.DeleteAccount(Now.AddDays(-1));
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_deleted",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_deleted_initial",
            "pm_deleted",
            "price_monthly",
            "monthly",
            "past_due",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            false,
            null,
            null,
            null,
            "evt_deleted_initial",
            Now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                "evt_deleted_active",
                "payment.succeeded",
                "customer_deleted",
                "pay_deleted_active",
                "pm_deleted",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
                user.Id.Value));
        var handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        var result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("active", subscription.Status);
        Assert.Single(webhookEventRepository.Events);
        Assert.Single(paymentRepository.Payments);
        Assert.False(user.HasRole(RoleNames.Premium));
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(0, userRepository.UpdateCount);
    }

    [Fact]
    public async Task BillingRenewalService_ForDueSubscription_UpdatesSubscriptionAddsPaymentAndKeepsPremiumRole() {
        var premiumRole = Role.Create(RoleNames.Premium);
        var user = User.Create("premium@example.com", "hash");
        user.ReplaceRoles([premiumRole]);
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_renewal",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_initial",
            "pm_renewal",
            "price_monthly",
            "monthly",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            false,
            null,
            null,
            null,
            "evt_initial",
            Now.AddMonths(-1),
            "{\"initial\":true}");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            new BillingRecurringPaymentModel(
                "pay_renewed",
                "pm_renewal",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                "evt_renewed",
                7.99m,
                "USD",
                "{\"renewed\":true}"));
        var service = new BillingRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            new NoOpBillingTransactionRunner(),
            [renewalGateway],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Renewed);
        Assert.Equal(0, result.Failed);
        Assert.Equal("pay_renewed", subscription.ExternalSubscriptionId);
        Assert.Equal(Now.AddMonths(1), subscription.CurrentPeriodEndUtc);
        Assert.Equal("{\"renewed\":true}", subscription.ProviderMetadataJson);

        var payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Renewal, payment.Kind);
        Assert.Equal("pay_renewed", payment.ExternalPaymentId);
        Assert.Equal("pm_renewal", payment.ExternalPaymentMethodId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.True(user.HasRole(RoleNames.Premium));
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

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

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
            false,
            null,
            null,
            null,
            "evt_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddMonths(-1));
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

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Renewed);
        Assert.Equal(1, result.Failed);
        Assert.Equal("past_due", subscription.Status);
        Assert.Equal(Now.AddHours(1), subscription.NextBillingAttemptUtc);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.False(user.HasRole(RoleNames.Premium));
    }

    [Fact]
    public async Task BillingRenewalService_ForDeletedUserSubscription_SkipsProviderAndDisablesRenewal() {
        var user = User.Create("deleted-renewal@example.com", "hash");
        user.DeleteAccount(Now.AddDays(-1));
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_deleted_renewal",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_deleted_initial",
            "pm_deleted_renewal",
            "price_monthly",
            "monthly",
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            false,
            null,
            null,
            null,
            "evt_deleted_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddMonths(-1));
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var renewalGateway = new FakeRecurringBillingGateway(
            BillingProviderNames.YooKassa,
            new BillingRecurringPaymentModel(
                "pay_should_not_be_used",
                "pm_deleted_renewal",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                "evt_should_not_be_used",
                7.99m,
                "USD",
                null));
        var service = new BillingRenewalService(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            new NoOpBillingTransactionRunner(),
            [renewalGateway],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

        var result = await service.RenewDueSubscriptionsAsync(BillingProviderNames.YooKassa, 10, CancellationToken.None);

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
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddDays(-1));
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
    public async Task GetBillingOverview_WithExistingSubscription_ReturnsBillingTimelineAndRenewalState() {
        var premiumRole = Role.Create(RoleNames.Premium);
        var user = User.Create("premium@example.com", "hash");
        user.ReplaceRoles([premiumRole]);
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_789",
            "price_yearly",
            "yearly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "pay_789",
            "pm_789",
            "price_yearly",
            "yearly",
            "active",
            Now,
            Now.AddYears(1),
            false,
            Now.AddYears(1),
            null,
            null,
            "evt_789",
            Now,
            "{\"provider\":\"yookassa\"}");
        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        var result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPremium);
        Assert.Equal(BillingProviderNames.YooKassa, result.Value.SubscriptionProvider);
        Assert.Equal("yearly", result.Value.Plan);
        Assert.Equal("active", result.Value.SubscriptionStatus);
        Assert.Equal(Now, result.Value.CurrentPeriodStartUtc);
        Assert.Equal(Now.AddYears(1), result.Value.CurrentPeriodEndUtc);
        Assert.Equal(Now.AddYears(1), result.Value.NextBillingAttemptUtc);
        Assert.True(result.Value.RenewalEnabled);
        Assert.False(result.Value.ManageBillingAvailable);
    }

    [Fact]
    public async Task GetBillingOverview_WithExpiredProviderTrial_DoesNotGrantPremium() {
        var user = User.Create("expired-provider-trial@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_trial_expired",
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Paddle,
            "sub_trial_expired",
            null,
            "price_monthly",
            "monthly",
            "trialing",
            Now.AddDays(-8),
            Now.AddDays(-1),
            false,
            null,
            Now.AddDays(-8),
            Now.AddDays(-1),
            "evt_trial_expired",
            Now.AddDays(-1));
        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        var result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPremium);
        Assert.Null(result.Value.SubscriptionStatus);
        Assert.Null(result.Value.CurrentPeriodStartUtc);
        Assert.Null(result.Value.CurrentPeriodEndUtc);
        Assert.True(result.Value.CanStartPremiumTrial);
    }

    [Fact]
    public async Task CreatePortalSession_UsesSubscriptionProviderInsteadOfActiveProvider() {
        var user = User.Create("portal@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_portal",
            "price_monthly",
            "monthly");
        var paddleGateway = new FakeBillingProviderGateway(
            BillingProviderNames.Paddle,
            portalSession: new BillingPortalSessionModel("https://billing.example/portal"));
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingProviderGatewayAccessor(
                activeProvider: new FakeBillingProviderGateway(BillingProviderNames.YooKassa),
                paddleGateway));

        var result = await handler.Handle(new CreatePortalSessionCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://billing.example/portal", result.Value.Url);
    }

    [Fact]
    public async Task StartPremiumTrial_ForEligibleUser_SetsTrialAndReturnsTrialOverview() {
        var user = User.Create("trial@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var handler = new StartPremiumTrialCommandHandler(
            userRepository,
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        var result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, user.PremiumTrialStartedAtUtc);
        Assert.Equal(Now.AddDays(7), user.PremiumTrialEndsAtUtc);
        Assert.True(user.HasActivePremiumTrial(Now));
        Assert.Equal("trialing", result.Value.SubscriptionStatus);
        Assert.True(result.Value.IsPremium);
        Assert.True(result.Value.PremiumTrialActive);
        Assert.True(result.Value.PremiumTrialUsed);
        Assert.False(result.Value.CanStartPremiumTrial);
        Assert.Equal(1, userRepository.UpdateCount);
    }

    [Fact]
    public async Task StartPremiumTrial_WhenAlreadyUsed_ReturnsConflict() {
        var user = User.Create("trial-used@example.com", "hash");
        user.StartPremiumTrial(Now.AddDays(-8), TimeSpan.FromDays(7));
        var handler = new StartPremiumTrialCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        var result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.TrialAlreadyUsed", result.Error.Code);
    }

    private static ProcessBillingWebhookCommandHandler CreateWebhookHandler(
        IBillingProviderGateway gateway,
        FakeUserRepository userRepository,
        InMemoryBillingSubscriptionRepository subscriptionRepository,
        RecordingBillingPaymentRepository paymentRepository,
        RecordingBillingWebhookEventRepository webhookEventRepository) {
        var dateTimeProvider = new FixedDateTimeProvider(Now);
        return new ProcessBillingWebhookCommandHandler(
            new FakeBillingProviderGatewayAccessor(gateway, gateway),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository,
            new NoOpBillingTransactionRunner(),
            userRepository,
            new BillingAccessService(userRepository, subscriptionRepository, dateTimeProvider),
            dateTimeProvider);
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository {
        private readonly List<User> _users = users.ToList();
        private readonly Role _premiumRole = Role.Create(RoleNames.Premium);

        public int UpdateCount { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                IsAccessible(user) &&
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => IsAccessible(user) && user.Id == id));

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(user => user.Id == id));

        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(
            long telegramUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<User> Items, int TotalItems)>((_users, _users.Count));

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            Task.FromResult((_users.Count, _users.Count, 0, 0, (IReadOnlyList<User>)_users.Take(recentLimit).ToList()));

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<Role> roles = names.Contains(RoleNames.Premium, StringComparer.Ordinal)
                ? [_premiumRole]
                : [];
            return Task.FromResult(roles);
        }

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) {
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            UpdateCount++;
            return Task.CompletedTask;
        }

        private static bool IsAccessible(User user) => user is { IsActive: true, DeletedAt: null };
    }

    private sealed class InMemoryBillingSubscriptionRepository(params BillingSubscription[] subscriptions)
        : IBillingSubscriptionRepository {
        public List<BillingSubscription> Subscriptions { get; } = subscriptions.ToList();
        public int UpdateCount { get; private set; }

        public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription => subscription.UserId == userId));

        public Task<BillingSubscription?> GetByExternalCustomerIdAsync(
            string provider,
            string externalCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalCustomerId, externalCustomerId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
            string provider,
            string externalSubscriptionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalSubscriptionId, externalSubscriptionId, StringComparison.Ordinal)));

        public Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
            string provider,
            string externalPaymentMethodId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Subscriptions.FirstOrDefault(subscription =>
                string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(subscription.ExternalPaymentMethodId, externalPaymentMethodId, StringComparison.Ordinal)));

        public Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
            string provider,
            DateTime dueAtUtc,
            int limit,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<BillingSubscription> dueSubscriptions = Subscriptions
                .Where(subscription =>
                    string.Equals(subscription.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                    subscription.NextBillingAttemptUtc.HasValue &&
                    subscription.NextBillingAttemptUtc <= dueAtUtc)
                .Take(limit)
                .ToList();
            return Task.FromResult(dueSubscriptions);
        }

        public Task<BillingSubscription> AddAsync(
            BillingSubscription subscription,
            CancellationToken cancellationToken = default) {
            Subscriptions.Add(subscription);
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBillingPaymentRepository : IBillingPaymentRepository {
        public List<BillingPayment> Payments { get; } = [];

        public Task<BillingPayment?> GetByExternalPaymentIdAsync(
            string provider,
            string externalPaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(payment =>
                string.Equals(payment.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.ExternalPaymentId, externalPaymentId, StringComparison.Ordinal)));

        public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
            Payments.Add(payment);
            return Task.FromResult(payment);
        }
    }

    private sealed class RecordingBillingWebhookEventRepository : IBillingWebhookEventRepository {
        public HashSet<string> ProcessedEventIds { get; } = new(StringComparer.Ordinal);
        public List<BillingWebhookEvent> Events { get; } = [];
        public bool ThrowAlreadyProcessedOnAdd { get; init; }

        public Task<bool> ExistsAsync(string provider, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProcessedEventIds.Contains(eventId));

        public Task<BillingWebhookEvent> AddAsync(
            BillingWebhookEvent webhookEvent,
            CancellationToken cancellationToken = default) {
            if (ThrowAlreadyProcessedOnAdd) {
                throw new BillingWebhookEventAlreadyProcessedException(webhookEvent.Provider, webhookEvent.EventId);
            }

            ProcessedEventIds.Add(webhookEvent.EventId);
            Events.Add(webhookEvent);
            return Task.FromResult(webhookEvent);
        }
    }

    private sealed class FakeBillingPublicConfigProvider : IBillingPublicConfigProvider {
        public BillingPublicConfigModel GetPublicConfig() =>
            new(BillingProviderNames.Paddle, "test_client_token", [BillingProviderNames.Paddle, BillingProviderNames.YooKassa]);
    }

    private sealed class FakeBillingProviderGatewayAccessor(
        IBillingProviderGateway activeProvider,
        params IBillingProviderGateway[] providers)
        : IBillingProviderGatewayAccessor {
        private readonly Dictionary<string, IBillingProviderGateway> _providers = providers
            .Concat([activeProvider])
            .GroupBy(provider => provider.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        public IBillingProviderGateway GetActiveProvider() => activeProvider;

        public IBillingProviderGateway? GetProviderOrDefault(string provider) =>
            _providers.GetValueOrDefault(provider);
    }

    private sealed class FakeBillingProviderGateway(
        string provider,
        BillingCheckoutSessionModel? checkoutSession = null,
        BillingWebhookEventModel? webhookEvent = null,
        BillingPortalSessionModel? portalSession = null)
        : IBillingProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
            BillingCheckoutSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(checkoutSession ?? new BillingCheckoutSessionModel(
                "session",
                "https://checkout.example/session",
                request.ExistingCustomerId ?? "customer",
                "price",
                request.Plan)));

        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
            BillingPortalSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(portalSession ?? new BillingPortalSessionModel(
                $"https://billing.example/{request.CustomerId}")));

        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<BillingWebhookEventModel?>(webhookEvent));
    }

    private sealed class FakeRecurringBillingGateway(
        string provider,
        BillingRecurringPaymentModel renewal)
        : IBillingRecurringProviderGateway {
        public string Provider { get; } = provider;
        public int CreatePaymentCallCount { get; private set; }

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) {
            CreatePaymentCallCount++;
            return Task.FromResult(Result.Success(renewal));
        }
    }

    private sealed class FailingRecurringBillingGateway(string providerName) : IBillingRecurringProviderGateway {
        private readonly string _provider = providerName;
        public string Provider => _provider;
        public int CreatePaymentCallCount { get; private set; }

        public Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
            BillingRecurringPaymentRequestModel request,
            CancellationToken cancellationToken = default) {
            CreatePaymentCallCount++;
            return Task.FromResult(Result.Failure<BillingRecurringPaymentModel>(
                Errors.Billing.ProviderOperationFailed(_provider, "declined")));
        }
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class NoOpBillingTransactionRunner : IBillingTransactionRunner {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
