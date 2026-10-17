using FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Models;
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
}
