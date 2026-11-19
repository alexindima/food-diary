using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Contracts.Commands.StartUserPremiumTrial;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests.Commands.StartUserPremiumTrial;

[ExcludeFromCodeCoverage]
public sealed class StartUserPremiumTrialCommandHandlerTests {
    private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(-1, true)]
    [InlineData(-14, false)]
    public async Task PreviouslyStartedTrial_ReturnsAlreadyUsedForBothActiveAndExpiredTrials(int startedDaysAgo, bool active) {
        User user = CreateConfirmedUser();
        user.StartPremiumTrial(Now.AddDays(startedDaysAgo), TimeSpan.FromDays(7));
        Assert.Equal(active, user.HasActivePremiumTrial(Now));

        await AssertAlreadyUsedTrialIsRejectedAsync(user);
    }

    [Fact]
    public async Task HistoricalEndWithoutStart_ReturnsAlreadyUsedWithoutReplacingThePersistedEnd() {
        User user = CreateConfirmedUser();
        // Nullable trial columns can be materialized independently from historical rows.
        typeof(User).GetProperty(nameof(User.PremiumTrialEndsAtUtc))!.SetValue(user, Now.AddDays(-1));
        Assert.False(user.HasUsedPremiumTrial());

        await AssertAlreadyUsedTrialIsRejectedAsync(user);
    }

    [Fact]
    public async Task TelegramRestrictedAccount_ReturnsAlreadyUsedBeforeRedeemingTheIdentity() {
        var user = User.CreateTelegram(456, "hash", premiumTrialRestricted: true);
        Assert.Multiple(
            () => Assert.Null(user.PremiumTrialStartedAtUtc),
            () => Assert.Null(user.PremiumTrialEndsAtUtc),
            () => Assert.True(user.PremiumTrialRestrictedByTelegram));

        await AssertAlreadyUsedTrialIsRejectedAsync(user);
    }

    private static async Task AssertAlreadyUsedTrialIsRejectedAsync(User user) {
        DateTime? originalStart = user.PremiumTrialStartedAtUtc;
        DateTime? originalEnd = user.PremiumTrialEndsAtUtc;
        bool originalRestriction = user.PremiumTrialRestrictedByTelegram;
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        IUserLookupRepository reads = Substitute.For<IUserLookupRepository>();
        reads.GetByIdAsync(user.Id, token).Returns(user);
        IUserWriteRepository writes = Substitute.For<IUserWriteRepository>();
        IUserPremiumTrialStore trials = Substitute.For<IUserPremiumTrialStore>();
        trials.TryRedeemTelegramAsync(Arg.Any<long>(), token).Returns(returnThis: true);
        var handler = new StartUserPremiumTrialCommandHandler(reads, writes, trials);

        Result<UserBillingProfileModel> result = await handler.Handle(
            new StartUserPremiumTrialCommand(user.Id, Now, TimeSpan.FromDays(7)), token);

        ResultAssert.Failure(result, UserErrors.PremiumTrialAlreadyUsed.Code);
        Assert.Multiple(
            () => Assert.Equal(originalStart, user.PremiumTrialStartedAtUtc),
            () => Assert.Equal(originalEnd, user.PremiumTrialEndsAtUtc),
            () => Assert.Equal(originalRestriction, user.PremiumTrialRestrictedByTelegram));
        await trials.Received(1).LockAccountAsync(user.Id, token);
        await reads.Received(1).GetByIdAsync(user.Id, token);
        await trials.DidNotReceiveWithAnyArgs().TryRedeemTelegramAsync(default, default);
        await writes.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    private static User CreateConfirmedUser() {
        var user = User.Create("trial-history@example.com", "hash");
        user.SetEmailConfirmed(isConfirmed: true);
        return user;
    }
}
