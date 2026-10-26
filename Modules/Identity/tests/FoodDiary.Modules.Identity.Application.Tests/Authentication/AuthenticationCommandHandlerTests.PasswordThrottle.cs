using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.Login;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    [Fact]
    public async Task PasswordThrottle_BlocksBeforePasswordVerificationAsync() {
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        throttle.TryBeginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new PasswordLoginAttempt("key", OwnerToken: null, RetryAfterSeconds: 30));
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        var tokens = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(users, new StubDateTimeProvider(), tokens, throttle);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(ErrorKind.RateLimited, result.Error.Kind),
            () => Assert.Equal("RateLimit.Exceeded", result.Error.Code),
            () => Assert.Equal("30", result.Error.Details!["retryAfterSeconds"][0]),
            () => Assert.Null(tokens.LastPrincipal));
        await users.DidNotReceiveWithAnyArgs().AuthenticatePasswordAsync(default!, default!, default, default);
    }

    [Theory]
    [InlineData("Authentication.InvalidCredentials", PasswordLoginAttemptOutcome.InvalidCredentials)]
    [InlineData("Authentication.AccountDeleted", PasswordLoginAttemptOutcome.Abandoned)]
    [InlineData("Dependency.Unavailable", PasswordLoginAttemptOutcome.Abandoned)]
    public async Task PasswordThrottle_CountsOnlyCredentialFailuresAsync(string code, PasswordLoginAttemptOutcome outcome) {
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        users.AuthenticatePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(new Error(code, "failure")));
        var handler = new LoginCommandHandler(users, new StubDateTimeProvider(), new StubAuthenticationTokenService(), throttle);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None);

        Assert.Equal(code, result.Error.Code);
        await throttle.Received(1).CompleteAsync(Arg.Any<PasswordLoginAttempt>(), outcome, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordThrottle_SuccessClearsFailuresBeforeIssuingTokensAsync() {
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        var user = User.Create("user@example.com", "secret");
        var handler = new LoginCommandHandler(CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(), new StubAuthenticationTokenService(), throttle);

        ResultAssert.Success(await handler.Handle(new LoginCommand("user@example.com", "secret"), CancellationToken.None));

        await throttle.Received(1).CompleteAsync(Arg.Any<PasswordLoginAttempt>(), PasswordLoginAttemptOutcome.Succeeded, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordThrottle_DependencyException_ReleasesLeaseWithoutCountingFailureAsync() {
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        users.AuthenticatePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Result<UserAuthenticationPrincipalModel>>(new InvalidOperationException("dependency failed")));
        var handler = new LoginCommandHandler(users, new StubDateTimeProvider(), new StubAuthenticationTokenService(), throttle);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None));

        await throttle.Received(1).CompleteAsync(Arg.Any<PasswordLoginAttempt>(), PasswordLoginAttemptOutcome.Abandoned, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PasswordThrottle_CancellationAfterVerification_ReleasesWithoutCountingFailureAsync() {
        using var cancellation = new CancellationTokenSource();
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        IUserAuthenticationIdentityService users = Substitute.For<IUserAuthenticationIdentityService>();
        users.AuthenticatePasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                await cancellation.CancelAsync();
                return Result.Failure<UserAuthenticationPrincipalModel>(AuthenticationErrors.InvalidCredentials);
            });
        var handler = new LoginCommandHandler(users, new StubDateTimeProvider(), new StubAuthenticationTokenService(), throttle);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(new LoginCommand("user@example.com", "wrong"), cancellation.Token));

        await throttle.Received(1).CompleteAsync(Arg.Any<PasswordLoginAttempt>(), PasswordLoginAttemptOutcome.Abandoned,
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
    }

    [Fact]
    public async Task PasswordThrottle_ExpiredLease_DoesNotIssueTokensAsync() {
        IPasswordLoginThrottle throttle = CreatePasswordLoginThrottle();
        throttle.CompleteAsync(Arg.Any<PasswordLoginAttempt>(), Arg.Any<PasswordLoginAttemptOutcome>(), Arg.Any<CancellationToken>()).Returns(returnThis: false);
        var user = User.Create("user@example.com", "secret");
        var tokens = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(), tokens, throttle);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand("user@example.com", "secret"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(ErrorKind.RateLimited, result.Error.Kind);
        Assert.Null(tokens.LastPrincipal);
    }
}
