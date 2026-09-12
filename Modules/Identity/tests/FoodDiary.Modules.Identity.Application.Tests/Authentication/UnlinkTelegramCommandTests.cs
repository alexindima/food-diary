using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Commands.UnlinkTelegram;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class UnlinkTelegramCommandTests {
    [Theory]
    [InlineData(0, true, true, true, true)]
    [InlineData(-6, true, true, true, false)]
    [InlineData(1, true, true, true, false)]
    [InlineData(0, false, true, true, false)]
    [InlineData(0, true, false, true, false)]
    [InlineData(0, true, true, false, false)]
    public async Task Unlink_RequiresFreshOwnedUnusedProof(int minutesOffset, bool matchingOwner, bool unused, bool accountAllowsUnlink, bool succeeds) {
        DateTime now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var user = User.CreateTelegram(123, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, now);
        ITelegramAuthValidator validator = Substitute.For<ITelegramAuthValidator>();
        validator.ValidateInitData("signed-proof").Returns(Result.Success(new TelegramInitData(123, Username: null,
            FirstName: null, LastName: null, PhotoUrl: null, LanguageCode: null, now.AddMinutes(minutesOffset))));
        ITelegramAssertionReplayGuard replay = Substitute.For<ITelegramAssertionReplayGuard>();
        replay.TryConsumeAsync("signed-proof", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(unused);
        IUserAuthenticationIdentityService identities = Substitute.For<IUserAuthenticationIdentityService>();
        identities.AuthenticateTelegramAsync(123, now, Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        IUserTelegramAccountService accounts = Substitute.For<IUserTelegramAccountService>();
        accounts.UnlinkAsync(user.Id, 123, principal.SecurityVersion, Arg.Any<CancellationToken>()).Returns(accountAllowsUnlink
            ? Result.Success() : Result.Failure(new Error("User.LastLoginMethod", "Keep a working login method.")));
        ITelegramOperationStore operations = Substitute.For<ITelegramOperationStore>();
        IPostCommitActionQueue postCommit = Substitute.For<IPostCommitActionQueue>();
        Func<CancellationToken, Task>? afterCommit = null;
        postCommit.When(queue => queue.Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => afterCommit = call.ArgAt<Func<CancellationToken, Task>>(1));
        var handler = new UnlinkTelegramCommandHandler(validator, replay, identities, accounts, new Clock(now), operations, postCommit);

        Result result = await handler.Handle(new UnlinkTelegramCommand(matchingOwner ? user.Id.Value : Guid.NewGuid(), "signed-proof"), CancellationToken.None);

        Assert.Equal(succeeds, result.IsSuccess);
        await accounts.Received(succeeds || !accountAllowsUnlink ? 1 : 0).UnlinkAsync(Arg.Any<UserId>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await operations.DidNotReceive().CancelUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        if (succeeds) {
            Assert.NotNull(afterCommit);
            await afterCommit(CancellationToken.None);
            await operations.Received(1).CancelUserAsync(user.Id.Value, CancellationToken.None);
        } else {
            Assert.Null(afterCommit);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class Clock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
