using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.DisconnectTelegram;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Errors;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TelegramDisconnect_IssuesFromUpdatedPrincipalAndCancelsAfterCommit(bool google) {
        var clock = new StubDateTimeProvider();
        var user = User.Create("disconnect@example.com", "secret");
        user.SetEmailConfirmed(isConfirmed: true);
        UserAuthenticationPrincipalModel before = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, clock.GetUtcNow().UtcDateTime);
        UserAuthenticationPrincipalModel after = before with { SecurityVersion = before.SecurityVersion + 1 };
        IUserTelegramAccountService accounts = Substitute.For<IUserTelegramAccountService>();
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        IGoogleTokenValidator validator = Substitute.For<IGoogleTokenValidator>();
        IAuthenticationTokenService tokens = Substitute.For<IAuthenticationTokenService>();
        ITelegramOperationStore operations = Substitute.For<ITelegramOperationStore>();
        IPostCommitActionQueue postCommit = Substitute.For<IPostCommitActionQueue>();
        var context = new AuthenticationClientContext("telegram-disconnect", IpAddress: null, UserAgent: null, CredentialKind: AuthenticationCredentialKind.Browser);
        string provider = google ? "google" : "password";
        CancellationToken ct = CancellationToken.None;
        users.GetAuthenticationPrincipalAsync(user.Id, Arg.Any<DateTime>(), ct).Returns(Result.Success(before));
        users.AuthenticatePasswordAsync("disconnect@example.com", "secret", Arg.Any<DateTime>(), ct).Returns(Result.Success(before));
        validator.ValidateCredentialAsync("linked-google", ct).Returns(Result.Success(
            new GoogleIdentityPayload("https://accounts.google.com", "subject", "disconnect@example.com", FirstName: null, LastName: null, Locale: null)));
        users.AuthenticateLinkedGoogleAsync(Arg.Any<UserGoogleAuthenticationModel>(), Arg.Any<DateTime>(), ct).Returns(Result.Success(before));
        accounts.DisconnectAsync(user.Id, before.SecurityVersion, ct).Returns(Result.Success(after));
        tokens.IssueFromPrincipalAsync(after, ct, context with { AuthProvider = provider }, rememberMe: false)
            .Returns(new IssuedAuthenticationTokens("access", "refresh"));
        Func<CancellationToken, Task>? afterCommit = null;
        postCommit.When(queue => queue.Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => afterCommit = call.ArgAt<Func<CancellationToken, Task>>(1));
        var handler = new DisconnectTelegramCommandHandler(accounts, users,
            new PasswordAuthenticationService(users, clock, CreatePasswordLoginThrottle()), validator, tokens, operations, postCommit, clock);

        Result<AuthenticationModel> result = await handler.Handle(new DisconnectTelegramCommand(user.Id.Value, before.SecurityVersion,
            google ? null : "secret", google ? "linked-google" : null, context, RememberMe: false), ct);

        ResultAssert.Success(result);
        await tokens.Received(1).IssueFromPrincipalAsync(after, ct, context with { AuthProvider = provider }, rememberMe: false);
        await operations.DidNotReceive().CancelUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        Assert.NotNull(afterCommit);
        await afterCommit(ct);
        await operations.Received(1).CancelUserAsync(user.Id, ct);
    }

    [Theory]
    [InlineData(null, 5L)]
    [InlineData("different-account", 5L)]
    [InlineData("different-account", null)]
    public async Task TelegramDisconnect_RejectsMissingOrDifferentAccountConfirmation(string? credential, long? version) {
        var clock = new StubDateTimeProvider();
        var user = User.Create("other@example.com", "secret");
        UserAuthenticationPrincipalModel other = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, clock.GetUtcNow().UtcDateTime);
        IUserTelegramAccountService accounts = Substitute.For<IUserTelegramAccountService>();
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        IGoogleTokenValidator validator = Substitute.For<IGoogleTokenValidator>();
        IAuthenticationTokenService tokens = Substitute.For<IAuthenticationTokenService>();
        ITelegramOperationStore operations = Substitute.For<ITelegramOperationStore>();
        IPostCommitActionQueue postCommit = Substitute.For<IPostCommitActionQueue>();
        CancellationToken ct = CancellationToken.None;
        validator.ValidateCredentialAsync("different-account", ct).Returns(Result.Success(
            new GoogleIdentityPayload("https://accounts.google.com", "other", "other@example.com", FirstName: null, LastName: null, Locale: null)));
        users.AuthenticateLinkedGoogleAsync(Arg.Any<UserGoogleAuthenticationModel>(), Arg.Any<DateTime>(), ct).Returns(Result.Success(other));
        var handler = new DisconnectTelegramCommandHandler(accounts, users,
            new PasswordAuthenticationService(users, clock, CreatePasswordLoginThrottle()), validator, tokens, operations, postCommit, clock);

        Result<AuthenticationModel> result = await handler.Handle(new DisconnectTelegramCommand(Guid.NewGuid(), version, Password: null, credential,
            new AuthenticationClientContext("telegram-disconnect", IpAddress: null, UserAgent: null), RememberMe: false), ct);

        Assert.True(result.IsFailure);
        if (version.HasValue) {
            Assert.Equal(credential is null ? IdentityErrors.ReauthenticationRequired.Code : IdentityErrors.ReauthenticationAccountMismatch.Code,
                result.Error.Code);
        }
        await accounts.DidNotReceive().DisconnectAsync(Arg.Any<UserId>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await tokens.DidNotReceiveWithAnyArgs().IssueFromPrincipalAsync(default!, default);
        postCommit.DidNotReceive().Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>());
    }
}
