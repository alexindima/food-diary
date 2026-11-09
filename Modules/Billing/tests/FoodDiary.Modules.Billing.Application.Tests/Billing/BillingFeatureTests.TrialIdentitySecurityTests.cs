using FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData("unconfirmed", false)]
    [InlineData("confirmed", true)]
    [InlineData("telegram", true)]
    public async Task TrialAndOverview_RequireVerifiedEmailOrEmailLessIdentity(string identity, bool eligible) {
        User user = identity is "telegram"
            ? User.CreateTelegram(123456789, "inaccessible-password-hash")
            : User.Create("trial-identity@example.com", "hash");
        if (identity is "confirmed") {
            user.SetEmailConfirmed(isConfirmed: true);
        }
        var users = new FakeUserRepository(user);
        var subscriptions = new InMemoryBillingSubscriptionRepository();
        var config = new FakeBillingPublicConfigProvider();
        var clock = new FixedDateTimeProvider(Now);
        GetBillingOverviewQueryHandler overview = CreateBillingOverviewHandler(users, subscriptions, config, clock);
        var trial = new StartPremiumTrialCommandHandler(users, subscriptions, config, clock);

        BillingOverviewModel before = ResultAssert.Success(await overview.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None));
        Result<BillingOverviewModel> result = await trial.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.Equal(eligible, before.CanStartPremiumTrial);
        if (eligible) {
            Assert.True(ResultAssert.Success(result).PremiumTrialActive);
            Assert.Equal(1, users.UpdateCount);
        } else {
            ResultAssert.Failure(result, UserErrors.EmailRequired.Code);
            Assert.Multiple(
                () => Assert.Null(user.PremiumTrialStartedAtUtc),
                () => Assert.Null(user.PremiumTrialEndsAtUtc),
                () => Assert.Equal(0, users.UpdateCount));
        }
    }

    [Fact]
    public async Task ExpiredCanceledStarsSubscription_DoesNotOfferOrStartATrial() {
        var user = User.CreateTelegram(123456789, "inaccessible-password-hash");
        BillingSubscription subscription = CreateSubscriptionSnapshot(user, BillingProviderNames.TelegramStars,
            "stars-customer", "stars-subscription", "stars-payment", "active", Now.AddDays(-30), Now.AddHours(-1), "stars-event", Now);
        SetPrivateProperty(subscription, nameof(BillingSubscription.CancelAtPeriodEnd), value: true);
        var users = new FakeUserRepository(user);
        var subscriptions = new InMemoryBillingSubscriptionRepository(subscription);
        var config = new FakeBillingPublicConfigProvider();
        var clock = new FixedDateTimeProvider(Now);
        GetBillingOverviewQueryHandler overview = CreateBillingOverviewHandler(users, subscriptions, config, clock);
        var trial = new StartPremiumTrialCommandHandler(users, subscriptions, config, clock);

        BillingOverviewModel current = ResultAssert.Success(await overview.Handle(new GetBillingOverviewQuery(user.Id.Value), CancellationToken.None));
        Result<BillingOverviewModel> attempt = await trial.Handle(new StartPremiumTrialCommand(user.Id.Value), CancellationToken.None);

        Assert.False(current.IsPremium);
        Assert.False(current.CanStartPremiumTrial);
        ResultAssert.Failure(attempt, "Billing.TrialUnavailable");
        Assert.Equal(0, users.UpdateCount);
    }
}
