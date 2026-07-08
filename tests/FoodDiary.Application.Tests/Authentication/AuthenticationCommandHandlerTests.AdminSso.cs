using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

    [Fact]
    public async Task AdminSsoStartHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AdminSsoStartCommandHandler(new StubAdminSsoService(), new StubUserRepository());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithNonAdminUser_ReturnsForbiddenWithoutCreatingCode() {
        var user = User.Create("user@example.com", "secret");
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository(user));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AdminSsoForbidden", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithMissingUser_ReturnsInvalidCredentials() {
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithAdminUser_CreatesCode() {
        var user = User.Create("admin@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository(user));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("admin-sso-code", result.Value.Code);
        Assert.Equal(1, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithInvalidCode_ReturnsFailure() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(),
            new StubUserRepository(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("bad-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AdminSsoInvalidCode", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithMissingUser_ReturnsNotFound() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(UserId.New()),
            new StubUserRepository(),
            new StubAuthenticationTokenService());

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
            new StubAdminSsoService(user.Id),
            new DirectUserByIdRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithNonAdminUser_ReturnsForbidden() {
        var user = User.Create("client@example.com", "secret");
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id),
            new StubUserRepository(user),
            new StubAuthenticationTokenService());

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
            new StubAdminSsoService(user.Id),
            new StubUserRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }
}
