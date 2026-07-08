using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.TestHelper;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {

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

        ResultAssert.Success(result);
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
            new CreateCheckoutSessionCommand(user.Id.Value, " Yearly ", Provider: null),
            CancellationToken.None);

        ResultAssert.Success(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task CreateCheckoutSession_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IBillingUserContextService userContextService = Substitute.For<IBillingUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var handler = new CreateCheckoutSessionCommandHandler(
            userContextService,
            new InMemoryBillingSubscriptionRepository(),
            new RecordingBillingPaymentRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)),
            new FixedDateTimeProvider(Now));

        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(userId.Value, "monthly", BillingProviderNames.Paddle),
            CancellationToken.None);

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Empty(paymentRepository.Payments);
    }

}
