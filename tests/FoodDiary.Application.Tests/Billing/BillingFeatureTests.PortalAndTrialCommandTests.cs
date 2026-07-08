using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Commands.StartPremiumTrial;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Billing.Models;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {

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

        ResultAssert.Success(result);
        Assert.Equal("https://billing.example/portal", result.Value.Url);
    }


    [Fact]
    public async Task CreatePortalSession_WhenUserIdIsInvalid_ReturnsInvalidToken() {
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task CreatePortalSession_WhenUserIsMissing_ReturnsInvalidToken() {
        var handler = new CreatePortalSessionCommandHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task CreatePortalSession_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IBillingUserContextService userContextService = Substitute.For<IBillingUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var handler = new CreatePortalSessionCommandHandler(
            userContextService,
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingProviderGatewayAccessor(new FakeBillingProviderGateway(BillingProviderNames.Paddle)));

        Result<BillingPortalSessionModel> result = await handler.Handle(new CreatePortalSessionCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Success(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task StartPremiumTrial_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IBillingUserContextService userContextService = Substitute.For<IBillingUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var handler = new StartPremiumTrialCommandHandler(
            userContextService,
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new StartPremiumTrialCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Success(result);
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

        ResultAssert.Success(result);
        Assert.True(result.Value.PremiumTrialActive);
        Assert.Equal(1, userRepository.UpdateCount);
    }

}
