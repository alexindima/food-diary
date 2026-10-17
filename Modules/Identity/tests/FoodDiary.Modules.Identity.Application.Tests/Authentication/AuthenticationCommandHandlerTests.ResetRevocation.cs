using FoodDiary.Modules.Identity.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Users.Application.Commands.ChangePassword;
using FoodDiary.Modules.Users.Application.Commands.SetPassword;
using FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    [Theory]
    [InlineData("change")]
    [InlineData("initial")]
    [InlineData("administrator")]
    public async Task PasswordReplacement_InvalidatesOutstandingResetProof(string operation) {
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var user = User.Create("reset-revocation@example.com", "old-password", hasPassword: !string.Equals(operation, "initial", StringComparison.Ordinal));
        user.SetEmailConfirmed(isConfirmed: true);
        user.SetPasswordResetToken(new UserTokenIssue("old-reset-token", now.AddHours(1), now));
        long previousVersion = user.SecurityVersion;
        var users = new StubUserRepository(user);
        var hasher = new StubPasswordHasher();
        IUserSessionRevocationService sessions = Substitute.For<IUserSessionRevocationService>();

        Result replacement = operation switch {
            "change" => await new ChangePasswordCommandHandler(users, hasher, sessions, clock)
                .Handle(new ChangePasswordCommand(user.Id.Value, "old-password", "replacement-password"), CancellationToken.None),
            "initial" => await new SetPasswordCommandHandler(users, hasher, sessions, clock)
                .Handle(new SetPasswordCommand(user.Id.Value, "replacement-password"), CancellationToken.None),
            _ => await new SetUserPasswordByAdministratorCommandHandler(users, users, hasher)
                .Handle(new SetUserPasswordByAdministratorCommand(user.Id, UserId.New(), "replacement-password"), CancellationToken.None),
        };
        ResultAssert.Success(replacement);
        var tokens = new StubAuthenticationTokenService();

        Result<AuthenticationModel> reset = await CreateConfirmPasswordResetHandler(users, clock, tokens)
            .Handle(new ConfirmPasswordResetCommand(user.Id.Value, "old-reset-token", "attacker-password"), CancellationToken.None);

        Assert.Equal("Authentication.InvalidToken", ResultAssert.Failure(reset).Code);
        Assert.Multiple(
            () => Assert.Equal("replacement-password", user.Password),
            () => Assert.Equal(previousVersion + 1, user.SecurityVersion),
            () => Assert.Null(user.PasswordResetTokenHash),
            () => Assert.Null(user.PasswordResetTokenExpiresAtUtc),
            () => Assert.Null(user.PasswordResetSentAtUtc),
            () => Assert.Null(tokens.LastPrincipal));
    }

    [Fact]
    public async Task PasswordHashMigration_PreservesOutstandingResetProofAndForcedChange() {
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var hasher = new MigratingPasswordHasher();
        var user = User.Create("rehash-reset@example.com", "legacy:password");
        user.RequirePasswordChange();
        user.SetPasswordResetToken(new UserTokenIssue(hasher.Hash("reset-token"), now.AddHours(1), now));
        var users = new StubUserRepository(user);
        UserAuthenticationIdentityService service = CreateUserAuthenticationIdentityService(users, hasher);

        Result<UserAuthenticationPrincipalModel> login = await service.AuthenticatePasswordAsync(
            Assert.IsType<string>(user.Email), "password", now, CancellationToken.None);

        ResultAssert.Success(login);
        Assert.Multiple(
            () => Assert.Equal("enhanced:password", user.Password),
            () => Assert.True(user.MustChangePassword),
            () => Assert.Equal(1, user.SecurityVersion),
            () => Assert.Equal("enhanced:reset-token", user.PasswordResetTokenHash),
            () => Assert.Equal(now.AddHours(1), user.PasswordResetTokenExpiresAtUtc),
            () => Assert.Equal(now, user.PasswordResetSentAtUtc));

        Result<UserAuthenticationPrincipalModel> reset = await service.CompletePasswordResetAsync(
            user.Id, "reset-token", "new-password", now, CancellationToken.None);
        ResultAssert.Success(reset);
        Result<UserAuthenticationPrincipalModel> replay = await service.CompletePasswordResetAsync(
            user.Id, "reset-token", "attacker-password", now, CancellationToken.None);

        Assert.Equal("Authentication.InvalidToken", ResultAssert.Failure(replay).Code);
        Assert.Equal("enhanced:new-password", user.Password);
    }
}
