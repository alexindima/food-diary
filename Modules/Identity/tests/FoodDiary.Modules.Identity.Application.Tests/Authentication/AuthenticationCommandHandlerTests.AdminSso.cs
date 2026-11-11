using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

    [Fact]
    public async Task AdminSsoStartHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AdminSsoStartCommandHandler(
            new StubAdminSsoService(),
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            new StubDateTimeProvider(), ConfirmedPrivilegedSessions());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithNonAdminUser_ReturnsForbiddenWithoutCreatingCode() {
        var user = User.Create("user@example.com", "secret");
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(
            adminSsoService,
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(), ConfirmedPrivilegedSessions(user.Id));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value, Guid.NewGuid(), user.SecurityVersion), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AdminSsoForbidden", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithMissingUser_ReturnsInvalidCredentials() {
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(
            adminSsoService,
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            new StubDateTimeProvider(), ConfirmedPrivilegedSessions());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdminSsoStartHandler_WithMissingOrStaleSession_DoesNotCreateCode(bool missingSession) {
        var user = User.Create("admin@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(
            adminSsoService, CreateUserAuthenticationIdentityService(new StubUserRepository(user)), new StubDateTimeProvider(), ConfirmedPrivilegedSessions(user.Id));
        var command = new AdminSsoStartCommand(user.Id.Value,
            missingSession ? Guid.Empty : Guid.NewGuid(), missingSession ? user.SecurityVersion : user.SecurityVersion - 1);

        Result<AdminSsoStartModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidCredentials");
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WhenPrincipalLookupFails_PreservesDependencyError() {
        IUserAuthenticationIdentityService identities = Substitute.For<IUserAuthenticationIdentityService>();
        var error = new Error("Dependency.Unavailable", "Principal lookup failed.");
        identities.GetAuthenticationPrincipalAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(error));
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, identities, new StubDateTimeProvider(), ConfirmedPrivilegedSessions());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Theory]
    [InlineData("password", 14, true)]
    [InlineData("google", 0, true)]
    [InlineData("password", 15, false)]
    [InlineData("telegram", 0, false)]
    [InlineData("admin-sso", 0, false)]
    public async Task AdminSsoStartHandler_RequiresFreshPasswordOrGoogle(string provider, int minutesAgo, bool expected) {
        var user = User.Create("admin@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(
            adminSsoService,
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(), ConfirmedPrivilegedSessions(user.Id, provider, minutesAgo));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value, Guid.NewGuid(), user.SecurityVersion), CancellationToken.None);

        if (expected) {
            ResultAssert.Success(result);
            Assert.Equal("admin-sso-code", result.Value.Code);
            Assert.Equal(1, adminSsoService.CreateCodeCallCount);
        } else {
            ResultAssert.Failure(result, "Authentication.ReauthenticationRequired");
            Assert.Equal(0, adminSsoService.CreateCodeCallCount);
        }
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithInvalidCode_ReturnsFailure() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(),
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(), ActiveSsoSessions(), InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("bad-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AdminSsoInvalidCode", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithMissingUser_ReturnsNotFound() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(UserId.New()),
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(), ActiveSsoSessions(), InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-admin-sso@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id, user.SecurityVersion),
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(),
            tokenService, ActiveSsoSessions(), InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions(user.Id));

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithNonAdminUser_ReturnsForbidden() {
        var user = User.Create("client@example.com", "secret");
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id, user.SecurityVersion),
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(), ActiveSsoSessions(), InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions(user.Id));

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AdminSsoForbidden", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithAdminUser_IssuesTokens() {
        var user = User.Create("admin-exchange@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id, user.SecurityVersion),
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(),
            tokenService, ActiveSsoSessions(), InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions(user.Id));

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Null(tokenService.LastUser);
        Assert.Equal(user.Id, tokenService.LastPrincipal?.UserId);
    }
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, -1)]
    public async Task AdminSsoExchangeHandler_RejectsRevokedSessionOrChangedSecurityVersion(bool sessionIsActive, int versionDelta) {
        var user = User.Create("revoked-admin-sso@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        IAdminSsoSessionLock sessions = Substitute.For<IAdminSsoSessionLock>();
        sessions.LockActiveForAdminSsoAsync(user.Id, Arg.Any<RefreshTokenSessionId>(), Arg.Any<CancellationToken>()).Returns(sessionIsActive);
        var tokens = new StubAuthenticationTokenService();
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id, user.SecurityVersion + versionDelta),
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(), tokens, sessions, InlineSsoExecutor(), Substitute.For<IUnitOfWork>(), ConfirmedPrivilegedSessions(user.Id));

        ResultAssert.Failure(await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None),
            "Authentication.AdminSsoInvalidCode");
        Assert.Null(tokens.LastPrincipal);
    }

    private static PrivilegedSessionService ConfirmedPrivilegedSessions(UserId? userId = null, string provider = "password", int minutesAgo = 0) {
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        IRefreshTokenSessionReadRepository sessions = Substitute.For<IRefreshTokenSessionReadRepository>();
        sessions.GetByIdAsync(Arg.Any<RefreshTokenSessionId>(), Arg.Any<CancellationToken>())
            .Returns(call => UserRefreshTokenSession.Create(
                call.Arg<RefreshTokenSessionId>(), userId ?? UserId.New(), "hash", rememberMe: false, provider, ipAddress: null, userAgent: null,
                now.AddMinutes(-minutesAgo), now.AddHours(1)));
        return new(sessions, clock);
    }

    private static IAdminSsoSessionLock ActiveSsoSessions() {
        IAdminSsoSessionLock sessions = Substitute.For<IAdminSsoSessionLock>();
        sessions.LockActiveForAdminSsoAsync(Arg.Any<UserId>(), Arg.Any<RefreshTokenSessionId>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        return sessions;
    }

    private static IAtomicCommandExecutor InlineSsoExecutor() {
        IAtomicCommandExecutor executor = Substitute.For<IAtomicCommandExecutor>();
        executor.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<AuthenticationModel>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Result<AuthenticationModel>>>>()(call.Arg<CancellationToken>()));
        return executor;
    }
}
