using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {

    [Fact]
    public async Task ProcessBillingWebhook_ForNewEvent_StoresSubscriptionPaymentWebhookAndAddsPremiumRole() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        const string providerMetadataJson = "{\"payment_id\":\"pay_456\"}";
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
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            7.99m,
            "USD",
            providerMetadataJson,
            user.Id.Value);
        var gateway = new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: webhookModel);
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{\"event\":\"payment.succeeded\"}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        BillingSubscription subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal("active", subscription.Status);
        Assert.Equal("pay_456", subscription.ExternalSubscriptionId);
        Assert.Equal("pm_456", subscription.ExternalPaymentMethodId);
        Assert.Equal(providerMetadataJson, subscription.ProviderMetadataJson);

        BillingPayment payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Webhook, payment.Kind);
        Assert.Equal("pay_456", payment.ExternalPaymentId);
        Assert.Equal(7.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal("evt_1", payment.WebhookEventId);

        BillingWebhookEvent webhookEvent = Assert.Single(webhookEventRepository.Events);
        Assert.Equal("evt_1", webhookEvent.EventId);
        Assert.Equal("processed", webhookEvent.Status);
        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.Equal(1, userRepository.RoleMembershipWriteCount);
        Assert.Equal(0, userRepository.UpdateCount);
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
                CancelAtPeriodEnd: false,
                CanceledAtUtc: null,
                TrialStartUtc: null,
                TrialEndUtc: null,
                7.99m,
                "USD",
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, userRepository.UpdateCount);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenProviderIsUnknown_ReturnsInvalidProvider() {
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.Paddle),
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand("unknown", "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.InvalidProvider", result.Error.Code);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenProviderRejectsWebhook_ReturnsProviderError() {
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookError: Errors.Billing.WebhookValidationFailed("bad signature")),
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", "bad"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenProviderIgnoresWebhook_ReturnsSuccessWithoutMutation() {
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa),
            new FakeUserRepository(),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenSubscriptionAlreadyHandledEvent_ReturnsSuccessWithoutMutation() {
        var user = User.Create("subscription-duplicate-event@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_duplicate_subscription",
            "pay_duplicate_subscription",
            "pm_duplicate_subscription",
            "active",
            Now.AddMonths(-1),
            Now.AddMonths(1),
            "evt_duplicate_subscription",
            Now.AddMonths(-1));
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookEvent: new BillingWebhookEventModel(
                    "evt_duplicate_subscription",
                    "payment.succeeded",
                    "customer_duplicate_subscription",
                    "pay_duplicate_subscription",
                    "pm_duplicate_subscription",
                    "price_monthly",
                    "monthly",
                    "active",
                    Now,
                    Now.AddMonths(1),
                    CancelAtPeriodEnd: false,
                    CanceledAtUtc: null,
                    TrialStartUtc: null,
                    TrialEndUtc: null,
                    7.99m,
                    "USD",
                    ProviderMetadataJson: null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, subscriptionRepository.UpdateCount);
    }

    [Fact]
    public async Task ProcessBillingWebhook_WhenEventIsOlderThanAppliedSnapshot_IgnoresStaleEvent() {
        var user = User.Create("stale-webhook@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_stale",
            "subscription_stale",
            "pm_stale",
            "active",
            Now,
            Now.AddMonths(1),
            "evt_newer",
            Now);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var staleEvent = new BillingWebhookEventModel(
            "evt_older",
            "subscription.canceled",
            "customer_stale",
            "subscription_stale",
            "pm_stale",
            "price_monthly",
            "monthly",
            "canceled",
            Now.AddMonths(-1),
            Now,
            CancelAtPeriodEnd: false,
            CanceledAtUtc: Now.AddDays(-1),
            TrialStartUtc: null,
            TrialEndUtc: null,
            Amount: null,
            Currency: null,
            ProviderMetadataJson: null,
            user.Id.Value,
            OccurredAtUtc: Now.AddMinutes(-1));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(BillingProviderNames.YooKassa, webhookEvent: staleEvent),
            new FakeUserRepository(user),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(0, subscriptionRepository.UpdateCount);
        Assert.Empty(paymentRepository.Payments);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenUserCannotBeResolved_ReturnsValidationFailure() {
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookEvent: new BillingWebhookEventModel(
                    "evt_missing_user",
                    "payment.succeeded",
                    "customer_missing_user",
                    "pay_missing_user",
                    "pm_missing_user",
                    "price_monthly",
                    "monthly",
                    "active",
                    Now,
                    Now.AddMonths(1),
                    CancelAtPeriodEnd: false,
                    CanceledAtUtc: null,
                    TrialStartUtc: null,
                    TrialEndUtc: null,
                    7.99m,
                    "USD",
                    ProviderMetadataJson: null,
                    UserId: null)),
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenConcurrentDuplicateInsertDetected_ReturnsSuccessWithoutMutation() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository {
            ThrowAlreadyProcessedOnAdd = true,
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
                CancelAtPeriodEnd: false,
                CanceledAtUtc: null,
                TrialStartUtc: null,
                TrialEndUtc: null,
                7.99m,
                "USD",
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, userRepository.UpdateCount);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenConcurrentDuplicatePaymentDetected_ReturnsSuccess() {
        var user = User.Create("duplicate-payment@example.com", "hash");
        var paymentRepository = new RecordingBillingPaymentRepository {
            ThrowAlreadyExistsOnAdd = true,
        };
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookEvent: new BillingWebhookEventModel(
                    "evt_duplicate_payment",
                    "payment.succeeded",
                    "customer_duplicate_payment",
                    "pay_duplicate_payment",
                    "pm_duplicate_payment",
                    "price_monthly",
                    "monthly",
                    "active",
                    Now,
                    Now.AddMonths(1),
                    CancelAtPeriodEnd: false,
                    CanceledAtUtc: null,
                    TrialStartUtc: null,
                    TrialEndUtc: null,
                    7.99m,
                    "USD",
                    ProviderMetadataJson: null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(paymentRepository.Payments);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenWebhookPaymentAlreadyExists_DoesNotAddDuplicatePayment() {
        var user = User.Create("existing-webhook-payment@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_existing_webhook_payment",
            "sub_existing_webhook_payment",
            "pm_existing_webhook_payment",
            "active",
            Now,
            Now.AddMonths(1),
            "evt_previous_webhook_payment",
            Now);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var existingPayment = BillingPayment.Create(
            user.Id,
            subscription.Id,
            BillingProviderNames.YooKassa,
            "sub_existing_webhook_payment",
            "customer_existing_webhook_payment",
            "sub_existing_webhook_payment",
            "pm_existing_webhook_payment",
            "price_monthly",
            "monthly",
            "active",
            BillingPaymentKinds.Webhook,
            7.99m,
            "USD",
            Now,
            Now.AddMonths(1),
            "evt_existing_payment_record",
            providerMetadataJson: null);
        var paymentRepository = new RecordingBillingPaymentRepository();
        paymentRepository.Payments.Add(existingPayment);
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookEvent: CreateWebhookPaymentEvent(user, "evt_existing_webhook_payment", "sub_existing_webhook_payment")),
            new FakeUserRepository(user),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(paymentRepository.Payments);
        Assert.Same(existingPayment, paymentRepository.Payments[0]);
        Assert.Single(webhookEventRepository.Events);
    }


    [Fact]
    public async Task ProcessBillingWebhook_WhenParsedEventIsIncomplete_ReturnsValidationFailureWithoutMutation() {
        var user = User.Create("invalid-webhook@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                string.Empty,
                "payment.succeeded",
                "customer_invalid",
                "pay_invalid",
                "pm_invalid",
                "price_monthly",
                "monthly",
                "active",
                Now,
                Now.AddMonths(1),
                CancelAtPeriodEnd: false,
                CanceledAtUtc: null,
                TrialStartUtc: null,
                TrialEndUtc: null,
                7.99m,
                "USD",
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Empty(paymentRepository.Payments);
        Assert.False(user.HasRole(RoleNames.Premium));
    }


    [Theory]
    [InlineData("event-type")]
    [InlineData("customer-id")]
    [InlineData("status")]
    public async Task ProcessBillingWebhook_WhenRequiredEventFieldIsMissing_ReturnsValidationFailure(string missingField) {
        var user = User.Create($"invalid-webhook-{missingField}@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.YooKassa,
            webhookEvent: new BillingWebhookEventModel(
                "evt_missing_field",
                string.Equals(missingField, "event-type", StringComparison.Ordinal) ? string.Empty : "payment.succeeded",
                string.Equals(missingField, "customer-id", StringComparison.Ordinal) ? " " : "customer_missing_field",
                "pay_missing_field",
                "pm_missing_field",
                "price_monthly",
                "monthly",
                string.Equals(missingField, "status", StringComparison.Ordinal) ? string.Empty : "active",
                Now,
                Now.AddMonths(1),
                CancelAtPeriodEnd: false,
                CanceledAtUtc: null,
                TrialStartUtc: null,
                TrialEndUtc: null,
                7.99m,
                "USD",
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
        Assert.Empty(subscriptionRepository.Subscriptions);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Empty(paymentRepository.Payments);
    }


    [Fact]
    public async Task ProcessBillingWebhook_ForManualPremiumUser_DoesNotRemovePremiumRoleOnCanceledSubscription() {
        User user = CreatePremiumUser("manual-premium@example.com");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            "customer_manual",
            "sub_manual",
            externalPaymentMethodId: null,
            "active",
            Now.AddDays(-1),
            Now.AddMonths(1),
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
                ExternalPaymentMethodId: null,
                "price_monthly",
                "monthly",
                "canceled",
                Now.AddDays(-1),
                Now,
                CancelAtPeriodEnd: false,
                Now,
                TrialStartUtc: null,
                TrialEndUtc: null,
                Amount: null,
                Currency: null,
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.Paddle, "{}", "signature"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.HasRole(RoleNames.Premium));
        Assert.False(subscription.PremiumRoleManagedByBilling);
    }


    [Fact]
    public async Task ProcessBillingWebhook_ForDeletedUserSubscription_StoresEventWithoutGrantingPremiumRole() {
        var user = User.Create("deleted-premium@example.com", "hash");
        user.DeleteAccount(Now.AddDays(-1));
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_deleted",
            "pay_deleted_initial",
            "pm_deleted",
            "past_due",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
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
                CancelAtPeriodEnd: false,
                CanceledAtUtc: null,
                TrialStartUtc: null,
                TrialEndUtc: null,
                7.99m,
                "USD",
                ProviderMetadataJson: null,
                user.Id.Value));
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("active", subscription.Status);
        Assert.Single(webhookEventRepository.Events);
        Assert.Single(paymentRepository.Payments);
        Assert.False(user.HasRole(RoleNames.Premium));
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(0, userRepository.UpdateCount);
    }


    [Fact]
    public async Task ProcessBillingWebhook_ForDeletedBillingManagedUser_DisablesManagedPremiumFlag() {
        var user = User.Create("deleted-managed-premium@example.com", "hash");
        user.DeleteAccount(Now.AddDays(-1));
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.YooKassa,
            "customer_deleted_managed",
            "pay_deleted_managed_initial",
            "pm_deleted_managed",
            "active",
            Now.AddMonths(-1),
            Now.AddMonths(1),
            "evt_deleted_managed_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(value: true, Now.AddMonths(-1));
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            new FakeBillingProviderGateway(
                BillingProviderNames.YooKassa,
                webhookEvent: new BillingWebhookEventModel(
                    "evt_deleted_managed_canceled",
                    "subscription.canceled",
                    "customer_deleted_managed",
                    "pay_deleted_managed_canceled",
                    "pm_deleted_managed",
                    "price_monthly",
                    "monthly",
                    "canceled",
                    Now.AddMonths(-1),
                    Now,
                    CancelAtPeriodEnd: false,
                    Now,
                    TrialStartUtc: null,
                    TrialEndUtc: null,
                    Amount: null,
                    Currency: null,
                    ProviderMetadataJson: null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            subscriptionRepository,
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(2, subscriptionRepository.UpdateCount);
    }

}
