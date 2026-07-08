using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Billing.Models;
using System.Globalization;

namespace FoodDiary.Application.Tests.Billing;

public partial class BillingFeatureTests {

    [Fact]
    public async Task BillingUserContextService_WithAccessibleUser_ForwardsRepositoryOperations() {
        var user = User.Create("billing-context@example.com", "hash");
        var repository = new FakeUserRepository(user);
        var roleMembershipService = new RecordingUserRoleMembershipService();
        var service = new BillingUserContextService(repository, repository, roleMembershipService);

        Result<User> accessible = await service.GetAccessibleUserAsync(user.Id, CancellationToken.None);
        User? includingDeleted = await service.GetUserIncludingDeletedAsync(user.Id, CancellationToken.None);
        bool canAccess = await service.CanAccessUserAsync(user, CancellationToken.None);
        await service.EnsurePremiumRoleAsync(user, CancellationToken.None);
        await service.RemovePremiumRoleAsync(user, CancellationToken.None);
        await service.UpdateUserAsync(user, CancellationToken.None);

        Assert.Same(user, ResultAssert.Success(accessible));
        Assert.Same(user, includingDeleted);
        Assert.True(canAccess);
        Assert.Equal([user.Id], roleMembershipService.EnsureRoleUserIds);
        Assert.Equal([user.Id], roleMembershipService.RemoveRoleUserIds);
        Assert.Equal(1, repository.UpdateCount);
    }


    [Fact]
    public async Task BillingUserContextService_WithMissingUser_ReturnsInvalidToken() {
        var repository = new FakeUserRepository();
        var service = new BillingUserContextService(repository, repository, new RecordingUserRoleMembershipService());

        Result<User> result = await service.GetAccessibleUserAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }


    [Fact]
    public async Task BillingUserContextService_GetAccessibleUserProfileAsync_ReturnsPremiumTrialState() {
        User user = CreatePremiumUser("billing-profile@example.com");
        user.StartPremiumTrial(Now, TimeSpan.FromDays(7));
        var repository = new FakeUserRepository(user);
        var service = new BillingUserContextService(repository, repository, new RecordingUserRoleMembershipService());

        Result<BillingUserProfileModel> result = await service.GetAccessibleUserProfileAsync(user.Id, CancellationToken.None);

        BillingUserProfileModel profile = ResultAssert.Success(result);
        Assert.True(profile.HasPaidPremium);
        Assert.Equal(Now, profile.PremiumTrialStartedAtUtc);
        Assert.Equal(Now.AddDays(7), profile.PremiumTrialEndsAtUtc);
    }


    [Fact]
    public async Task BillingUserContextService_GetAccessibleUserProfileAsync_WhenUserLoadFails_ReturnsFailure() {
        var userId = UserId.New();
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken)));
        var service = new BillingUserContextService(
            Substitute.For<IBillingUserLookupService>(),
            userContextService,
            new RecordingUserRoleMembershipService());

        Result<BillingUserProfileModel> result = await service.GetAccessibleUserProfileAsync(userId, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task BillingUserContextService_EnsureCanAccessAsync_ForwardsAccessFailure() {
        var user = User.Create("billing-access-deleted@example.com", "hash");
        user.DeleteAccount(Now);
        var repository = new FakeUserRepository(user);
        var service = new BillingUserContextService(repository, repository, new RecordingUserRoleMembershipService());

        Error? error = await service.EnsureCanAccessAsync(user.Id, CancellationToken.None);

        Assert.Equal("Authentication.InvalidToken", error?.Code);
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
            cancelAtPeriodEnd: false,
            Now.AddYears(1),
            trialStartUtc: null,
            trialEndUtc: null,
            "evt_789",
            Now,
            "{\"provider\":\"yookassa\"}");
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
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
            externalPaymentMethodId: null,
            "price_monthly",
            "monthly",
            "trialing",
            Now.AddDays(-8),
            Now.AddDays(-1),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            Now.AddDays(-8),
            Now.AddDays(-1),
            "evt_trial_expired",
            Now.AddDays(-1));
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(result.Value.IsPremium);
        Assert.Null(result.Value.SubscriptionStatus);
        Assert.Null(result.Value.CurrentPeriodStartUtc);
        Assert.Null(result.Value.CurrentPeriodEndUtc);
        Assert.True(result.Value.CanStartPremiumTrial);
    }


    [Fact]
    public async Task GetBillingOverview_WithInvalidUserId_ReturnsInvalidToken() {
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetBillingOverview_WhenUserIsMissing_ReturnsInvalidToken() {
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(),
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetBillingOverview_WhenProfileLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IBillingUserContextService userContextService = Substitute.For<IBillingUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<BillingUserProfileModel>(Errors.Authentication.InvalidToken)));
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            userContextService,
            new InMemoryBillingSubscriptionRepository(),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
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
        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
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

        GetBillingOverviewQueryHandler handler = CreateBillingOverviewHandler(
            new FakeUserRepository(user),
            new InMemoryBillingSubscriptionRepository(subscription),
            new FakeBillingPublicConfigProvider(),
            new FixedDateTimeProvider(Now));

        Result<BillingOverviewModel> result = await handler.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(expectedPremium, result.Value.IsPremium);
        Assert.Equal(expectedCanStartTrial, result.Value.CanStartPremiumTrial);
    }

}
