using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
using FoodDiary.Application.Billing.Models;
using System.Reflection;
using System.Globalization;

namespace FoodDiary.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
public sealed class BillingFeatureTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCheckoutSessionValidator_WithPaddedPlanAndProvider_ReturnsSuccess() {
        TestValidationResult<CreateCheckoutSessionCommand> result = await new CreateCheckoutSessionCommandValidator().TestValidateAsync(
            new CreateCheckoutSessionCommand(Guid.NewGuid(), " Monthly ", $" {BillingProviderNames.YooKassa} "));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateCheckoutSessionValidator_WithBlankProvider_ReturnsSuccess() {
        TestValidationResult<CreateCheckoutSessionCommand> result = await new CreateCheckoutSessionCommandValidator().TestValidateAsync(
            new CreateCheckoutSessionCommand(Guid.NewGuid(), "monthly", " "));

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

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, " Monthly ", $" {BillingProviderNames.YooKassa} "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example/pay_123", result.Value.Url);
        BillingSubscription subscription = Assert.Single(subscriptionRepository.Subscriptions);
        Assert.Equal(BillingProviderNames.YooKassa, subscription.Provider);
        Assert.Equal("customer_123", subscription.ExternalCustomerId);
        Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status);

        BillingPayment payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(BillingPaymentKinds.Checkout, payment.Kind);
        Assert.Equal("pay_123", payment.ExternalPaymentId);
        Assert.Equal(subscription.Id, payment.BillingSubscriptionId);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithExistingInactiveSubscription_UpdatesCheckoutContext() {
        var user = User.Create("existing-checkout@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            "customer_old",
            "sub_old",
            "pm_old",
            "canceled",
            Now.AddMonths(-2),
            Now.AddMonths(-1),
            "evt_canceled",
            Now.AddMonths(-1));
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository(subscription);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var gateway = new FakeBillingProviderGateway(
            BillingProviderNames.Paddle,
            checkoutSession: new BillingCheckoutSessionModel(
                "session_new",
                "https://checkout.example/session_new",
                "customer_new",
                "price_yearly",
                "yearly"));
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(user),
            subscriptionRepository,
            paymentRepository,
            new FakeBillingProviderGatewayAccessor(gateway),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, " Yearly ", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, subscriptionRepository.UpdateCount);
        Assert.Equal("customer_new", subscription.ExternalCustomerId);
        Assert.Equal("price_yearly", subscription.ExternalPriceId);
        Assert.Equal("yearly", subscription.Plan);
        BillingPayment payment = Assert.Single(paymentRepository.Payments);
        Assert.Equal(subscription.Id, payment.BillingSubscriptionId);
        Assert.Equal("session_new", payment.ExternalPaymentId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task CreateCheckoutSession_WithInvalidUserId_ReturnsInvalidToken(string? userIdValue) {
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)),
            new FixedDateTimeProvider(Now));
        Guid? userId = userIdValue is null ? null : Guid.Parse(userIdValue);

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(userId, "monthly", BillingProviderNames.Paddle),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenUserIsMissing_ReturnsInvalidToken() {
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(Guid.NewGuid(), "monthly", BillingProviderNames.Paddle),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("trialing")]
    [InlineData("past_due")]
    public async Task CreateCheckoutSession_WithActivePaidSubscription_ReturnsAlreadyActive(string status) {
        var user = User.Create($"{status}@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            $"customer_{status}",
            $"sub_{status}",
            $"pm_{status}",
            status,
            Now.AddDays(-1),
            Now.AddDays(1),
            $"evt_{status}",
            Now);
        var paymentRepository = new RecordingBillingPaymentRepository();
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            paymentRepository,
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.Paddle),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.SubscriptionAlreadyActive", result.Error.Code);
        Assert.Empty(paymentRepository.Payments);
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenProviderIsMissing_ReturnsProviderNotConfigured() {
        var user = User.Create("missing-provider@example.com", "hash");
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", "missing"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task CreateCheckoutSession_WhenProviderFails_ReturnsProviderError() {
        var user = User.Create("checkout-failure@example.com", "hash");
        var paymentRepository = new RecordingBillingPaymentRepository();
        var handler = new CreateCheckoutSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            paymentRepository,
            new FakeBillingProviderGatewayAccessor(
                new FakeBillingProviderGateway(
                    BillingProviderNames.Paddle,
                    checkoutError: Errors.Billing.ProviderOperationFailed(BillingProviderNames.Paddle, "declined"))),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.Paddle),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Empty(paymentRepository.Payments);
    }

    [Fact]
    public async Task ProcessBillingWebhook_ForNewEvent_StoresSubscriptionPaymentWebhookAndAddsPremiumRole() {
        var user = User.Create("premium@example.com", "hash");
        var userRepository = new FakeUserRepository(user);
        var subscriptionRepository = new InMemoryBillingSubscriptionRepository();
        var paymentRepository = new RecordingBillingPaymentRepository();
        var webhookEventRepository = new RecordingBillingWebhookEventRepository();
        string providerMetadataJson = "{\"payment_id\":\"pay_456\"}";
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
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{\"event\":\"payment.succeeded\"}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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

        Assert.True(result.IsFailure);
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

        Assert.True(result.IsFailure);
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

        Assert.True(result.IsSuccess);
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
                    false,
                    null,
                    null,
                    null,
                    7.99m,
                    "USD",
                    null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(paymentRepository.Payments);
        Assert.Empty(webhookEventRepository.Events);
        Assert.Equal(0, subscriptionRepository.UpdateCount);
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
                    false,
                    null,
                    null,
                    null,
                    7.99m,
                    "USD",
                    null,
                    null)),
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
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

        Assert.True(result.IsSuccess);
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
                    false,
                    null,
                    null,
                    null,
                    7.99m,
                    "USD",
                    null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            null);
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

        Assert.True(result.IsSuccess);
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
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
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

        Assert.True(result.IsFailure);
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
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
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

        Assert.True(result.IsFailure);
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
            null,
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
        ProcessBillingWebhookCommandHandler handler = CreateWebhookHandler(
            gateway,
            userRepository,
            subscriptionRepository,
            paymentRepository,
            webhookEventRepository);

        Result result = await handler.Handle(
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
                false,
                null,
                null,
                null,
                7.99m,
                "USD",
                null,
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

        Assert.True(result.IsSuccess);
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
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddMonths(-1));
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
                    false,
                    Now,
                    null,
                    null,
                    null,
                    null,
                    null,
                    user.Id.Value)),
            new FakeUserRepository(user),
            subscriptionRepository,
            new RecordingBillingPaymentRepository(),
            new RecordingBillingWebhookEventRepository());

        Result result = await handler.Handle(
            new ProcessBillingWebhookCommand(BillingProviderNames.YooKassa, "{}", string.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(2, subscriptionRepository.UpdateCount);
    }

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
            null));
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
            null,
            "active",
            Now.AddMonths(-1),
            Now.AddMinutes(-1),
            "evt_initial",
            Now.AddMonths(-1));
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddMonths(-1));
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
        subscription.MarkPremiumRoleManagedByBilling(true, Now.AddMonths(-1));
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

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

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

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPremium);
        Assert.Null(result.Value.SubscriptionStatus);
        Assert.Null(result.Value.CurrentPeriodStartUtc);
        Assert.Null(result.Value.CurrentPeriodEndUtc);
        Assert.True(result.Value.CanStartPremiumTrial);
    }

    [Fact]
    public async Task GetBillingOverview_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetBillingOverview_WhenUserIsMissing_ReturnsInvalidToken() {
        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetBillingOverview_WithBlankSubscriptionStatus_DoesNotGrantPaidPremium() {
        var user = User.Create("blank-status-overview@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            "customer_blank_status",
            "sub_blank_status",
            "pm_blank_status",
            "active",
            Now.AddDays(-1),
            Now.AddDays(1),
            "evt_blank_status",
            Now);
        SetPrivateProperty(subscription, nameof(BillingSubscription.Status), " ");
        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPremium);
        Assert.Equal(" ", result.Value.SubscriptionStatus);
        Assert.True(result.Value.CanStartPremiumTrial);
    }

    [Theory]
    [InlineData("trialing", 1, true, false)]
    [InlineData("past_due", null, true, false)]
    [InlineData("past_due", -1, false, true)]
    [InlineData("canceled", 1, false, true)]
    public async Task GetBillingOverview_WithPaidSubscriptionStatuses_ReportsPremiumState(
        string status,
        int? periodEndOffsetDays,
        bool expectedPremium,
        bool expectedCanStartTrial) {
        var user = User.Create(string.Create(CultureInfo.InvariantCulture, $"overview-status-{status}-{periodEndOffsetDays ?? 0}@example.com"), "hash");
        DateTime periodEnd = periodEndOffsetDays.HasValue ? Now.AddDays(periodEndOffsetDays.Value) : Now.AddDays(1);
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            string.Create(CultureInfo.InvariantCulture, $"customer_overview_{status}_{periodEndOffsetDays ?? 0}"),
            string.Create(CultureInfo.InvariantCulture, $"sub_overview_{status}_{periodEndOffsetDays ?? 0}"),
            string.Create(CultureInfo.InvariantCulture, $"pm_overview_{status}_{periodEndOffsetDays ?? 0}"),
            status,
            Now.AddDays(-1),
            periodEnd,
            string.Create(CultureInfo.InvariantCulture, $"evt_overview_{status}_{periodEndOffsetDays ?? 0}"),
            Now);
        if (!periodEndOffsetDays.HasValue) {
            SetPrivateProperty(subscription, nameof(BillingSubscription.CurrentPeriodEndUtc), (DateTime?)null);
        }

        var handler = new GetBillingOverviewQueryHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPremium, result.Value.IsPremium);
        Assert.Equal(expectedCanStartTrial, result.Value.CanStartPremiumTrial);
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

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://billing.example/portal", result.Value.Url);
    }

    [Fact]
    public async Task CreatePortalSession_WhenUserIdIsInvalid_ReturnsInvalidToken() {
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreatePortalSession_WhenUserIsMissing_ReturnsInvalidToken() {
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreatePortalSession_WhenSubscriptionIsMissing_ReturnsUnavailable() {
        var user = User.Create("portal-missing@example.com", "hash");
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.CustomerPortalUnavailable", result.Error.Code);
    }

    [Fact]
    public async Task CreatePortalSession_WhenProviderIsMissing_ReturnsUnavailable() {
        var user = User.Create("portal-provider-missing@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.YooKassa,
            "customer_missing",
            "price_monthly",
            "monthly");
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.CustomerPortalUnavailable", result.Error.Code);
    }

    [Fact]
    public async Task CreatePortalSession_WhenProviderFails_ReturnsProviderError() {
        var user = User.Create("portal-provider-failure@example.com", "hash");
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            BillingProviderNames.Paddle,
            "customer_failure",
            "price_monthly",
            "monthly");
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingProviderGatewayAccessor(
                new FakeBillingProviderGateway(
                    BillingProviderNames.Paddle,
                    portalError: Errors.Billing.ProviderOperationFailed(BillingProviderNames.Paddle, "portal failed"))));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
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

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

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

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.TrialAlreadyUsed", result.Error.Code);
    }

    [Fact]
    public async Task StartPremiumTrial_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new StartPremiumTrialCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task StartPremiumTrial_WhenUserIsMissing_ReturnsInvalidToken() {
        var handler = new StartPremiumTrialCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("trialing")]
    [InlineData("past_due")]
    public async Task StartPremiumTrial_WithActivePaidSubscription_ReturnsAlreadyActive(string status) {
        var user = User.Create($"trial-active-{status}@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            $"customer_trial_{status}",
            $"sub_trial_{status}",
            $"pm_trial_{status}",
            status,
            Now.AddDays(-1),
            Now.AddDays(1),
            $"evt_trial_{status}",
            Now);
        var userRepository = new FakeUserRepository(user);
        var handler = new StartPremiumTrialCommandHandler(
            userRepository,
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.SubscriptionAlreadyActive", result.Error.Code);
        Assert.Equal(0, userRepository.UpdateCount);
    }

    [Theory]
    [InlineData("trialing", -1)]
    [InlineData("past_due", -1)]
    [InlineData("canceled", 1)]
    public async Task StartPremiumTrial_WithInactivePaidSubscription_AllowsTrial(string status, int periodEndOffsetDays) {
        var user = User.Create($"trial-inactive-{status}@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            $"customer_trial_inactive_{status}",
            $"sub_trial_inactive_{status}",
            $"pm_trial_inactive_{status}",
            status,
            Now.AddDays(-2),
            Now.AddDays(periodEndOffsetDays),
            $"evt_trial_inactive_{status}",
            Now);
        var userRepository = new FakeUserRepository(user);
        var handler = new StartPremiumTrialCommandHandler(
            userRepository,
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.PremiumTrialActive);
        Assert.Equal(1, userRepository.UpdateCount);
    }

    [Fact]
    public async Task StartPremiumTrial_WithBlankSubscriptionStatus_AllowsTrial() {
        var user = User.Create("trial-blank-status@example.com", "hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(
            user,
            BillingProviderNames.Paddle,
            "customer_trial_blank",
            "sub_trial_blank",
            "pm_trial_blank",
            "active",
            Now.AddDays(-1),
            Now.AddDays(1),
            "evt_trial_blank",
            Now);
        SetPrivateProperty(subscription, nameof(BillingSubscription.Status), " ");
        var userRepository = new FakeUserRepository(user);
        var handler = new StartPremiumTrialCommandHandler(
            userRepository,
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.PremiumTrialActive);
        Assert.Equal(1, userRepository.UpdateCount);
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

    private static User CreatePremiumUser(string email) {
        var user = User.Create(email, "hash");
        user.ReplaceRoles([Role.Create(RoleNames.Premium)]);
        return user;
    }

    private static BillingSubscription CreateSubscriptionSnapshot(
        User user,
        string provider,
        string externalCustomerId,
        string? externalSubscriptionId,
        string? externalPaymentMethodId,
        string status,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        string eventId,
        DateTime eventCreatedAtUtc,
        string? metadataJson = null) {
        var subscription = BillingSubscription.CreatePending(
            user.Id,
            provider,
            externalCustomerId,
            "price_monthly",
            "monthly");
        subscription.ApplyProviderSnapshot(
            provider,
            externalSubscriptionId,
            externalPaymentMethodId,
            "price_monthly",
            "monthly",
            status,
            periodStartUtc,
            periodEndUtc,
            false,
            null,
            null,
            null,
            eventId,
            eventCreatedAtUtc,
            metadataJson);
        return subscription;
    }

    private static BillingRecurringPaymentModel CreateRenewalPayment(
        string externalPaymentId,
        string externalPaymentMethodId,
        string eventId,
        string? metadataJson = null) =>
        new(
            externalPaymentId,
            externalPaymentMethodId,
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            eventId,
            7.99m,
            "USD",
            metadataJson);

    private static BillingWebhookEventModel CreateWebhookPaymentEvent(User user, string eventId, string externalSubscriptionId) =>
        new(
            eventId,
            "payment.succeeded",
            "customer_existing_webhook_payment",
            externalSubscriptionId,
            "pm_existing_webhook_payment",
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
            user.Id.Value);

    private static BillingRenewalService CreateRenewalService(
        InMemoryBillingSubscriptionRepository subscriptionRepository,
        RecordingBillingPaymentRepository paymentRepository,
        FakeUserRepository userRepository,
        IBillingRecurringProviderGateway renewalGateway) =>
        new(
            subscriptionRepository,
            paymentRepository,
            userRepository,
            new NoOpBillingTransactionRunner(),
            [renewalGateway],
            new BillingAccessService(userRepository, subscriptionRepository, new FixedDateTimeProvider(Now)),
            new FixedDateTimeProvider(Now));

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        PropertyInfo? property = typeof(TTarget).GetProperty(propertyName);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class RecordingBillingPaymentRepository : IBillingPaymentRepository {
        public List<BillingPayment> Payments { get; } = [];
        public bool ThrowAlreadyExistsOnAdd { get; init; }

        public Task<BillingPayment?> GetByExternalPaymentIdAsync(
            string provider,
            string externalPaymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.FirstOrDefault(payment =>
                string.Equals(payment.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(payment.ExternalPaymentId, externalPaymentId, StringComparison.Ordinal)));

        public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
            if (ThrowAlreadyExistsOnAdd) {
                throw new BillingPaymentAlreadyExistsException(payment.Provider, payment.ExternalPaymentId);
            }

            Payments.Add(payment);
            return Task.FromResult(payment);
        }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class FakeBillingPublicConfigProvider : IBillingPublicConfigProvider {
        public BillingPublicConfigModel GetPublicConfig() =>
            new(BillingProviderNames.Paddle, "test_client_token", [BillingProviderNames.Paddle, BillingProviderNames.YooKassa]);
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class FakeBillingProviderGateway(
        string provider,
        BillingCheckoutSessionModel? checkoutSession = null,
        BillingWebhookEventModel? webhookEvent = null,
        BillingPortalSessionModel? portalSession = null,
        Error? checkoutError = null,
        Error? portalError = null,
        Error? webhookError = null)
        : IBillingProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
            BillingCheckoutSessionRequestModel request,
            CancellationToken cancellationToken = default) {
            if (checkoutError is not null) {
                return Task.FromResult(Result.Failure<BillingCheckoutSessionModel>(checkoutError));
            }

            return Task.FromResult(Result.Success(checkoutSession ?? new BillingCheckoutSessionModel(
                "session",
                "https://checkout.example/session",
                request.ExistingCustomerId ?? "customer",
                "price",
                request.Plan)));
        }

        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
            BillingPortalSessionRequestModel request,
            CancellationToken cancellationToken = default) {
            if (portalError is not null) {
                return Task.FromResult(Result.Failure<BillingPortalSessionModel>(portalError));
            }

            return Task.FromResult(Result.Success(portalSession ?? new BillingPortalSessionModel(
                $"https://billing.example/{request.CustomerId}")));
        }

        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default) {
            if (webhookError is not null) {
                return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(webhookError));
            }

            return Task.FromResult(Result.Success<BillingWebhookEventModel?>(webhookEvent));
        }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoOpBillingTransactionRunner : IBillingTransactionRunner {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
