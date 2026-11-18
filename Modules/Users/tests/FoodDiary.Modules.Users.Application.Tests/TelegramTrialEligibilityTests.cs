using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramTrialEligibilityTests {
    private const long TelegramUserId = 456;

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public async Task BillingProfile_ConsultsRedemptionOnlyForVerifiedTelegramLinks(
        bool verifiedLink, bool redeemed, bool expectedRestriction) {
        var userId = UserId.New();
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        var profile = new UserBillingProfileModel(userId, Email: null, IsActive: true, IsDeleted: false,
            HasPaidPremium: false, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null,
            TelegramUserId: TelegramUserId, HasVerifiedTelegramLink: verifiedLink);
        IUserBillingProfileReadModelRepository profiles = Substitute.For<IUserBillingProfileReadModelRepository>();
        profiles.GetBillingProfileIncludingDeletedAsync(userId, token).Returns(profile);
        IUserPremiumTrialStore trials = Substitute.For<IUserPremiumTrialStore>();
        trials.HasRedeemedTelegramAsync(TelegramUserId, token).Returns(redeemed);
        var handler = new GetUserBillingProfileQueryHandler(profiles, trials);

        UserBillingProfileModel result = ResultAssert.Success(await handler.Handle(new GetUserBillingProfileQuery(userId), token));

        Assert.Multiple(
            () => Assert.Equal(userId, result.UserId),
            () => Assert.Equal(expectedRestriction, result.PremiumTrialRestrictedByTelegram),
            () => Assert.Equal(expectedRestriction, result.HasUsedPremiumTrial),
            () => Assert.Null(result.PremiumTrialEndsAtUtc));
        await trials.Received(verifiedLink ? 1 : 0).HasRedeemedTelegramAsync(TelegramUserId, token);
    }

    [Fact]
    public async Task StartTrial_DoesNotGrantAccessWhenTheTelegramIdentityAlreadyRedeemedIt() {
        var user = User.CreateTelegram(TelegramUserId, "hash");
        IUserLookupRepository reads = Substitute.For<IUserLookupRepository>();
        reads.GetByIdAsync(user.Id, CancellationToken.None).Returns(user);
        IUserWriteRepository writes = Substitute.For<IUserWriteRepository>();
        IUserPremiumTrialStore trials = Substitute.For<IUserPremiumTrialStore>();
        trials.TryRedeemTelegramAsync(TelegramUserId, CancellationToken.None).Returns(returnThis: false);
        var handler = new StartUserPremiumTrialCommandHandler(reads, writes, trials);

        ResultAssert.Failure(await handler.Handle(
            new StartUserPremiumTrialCommand(user.Id, DateTime.UtcNow, TimeSpan.FromDays(7)), CancellationToken.None),
            UserErrors.PremiumTrialAlreadyUsed.Code);

        Assert.Null(user.PremiumTrialStartedAtUtc);
        await trials.Received(1).LockAccountAsync(user.Id, CancellationToken.None);
        await trials.Received(1).TryRedeemTelegramAsync(TelegramUserId, CancellationToken.None);
        await writes.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
