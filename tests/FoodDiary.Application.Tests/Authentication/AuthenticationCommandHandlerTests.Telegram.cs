using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

    [Fact]
    public async Task LinkTelegramHandler_WithEmptyUserId_ReturnsValidationFailure() {
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(new StubUserRepository());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(Guid.Empty, "init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenInitDataInvalid_ReturnsFailure() {
        var user = User.Create("link-invalid@example.com", "secret");
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator(validateFailure: true));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "bad-init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenAssertionWasConsumed_RejectsReplay() {
        var user = User.Create("link-replay@example.com", "secret");
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(
            new StubUserRepository(user),
            replayGuard: new StubTelegramAssertionReplayGuard(consume: false));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "replayed-init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramAssertionAlreadyUsed", result.Error.Code);
        Assert.Null(user.TelegramUserId);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenUserMissing_ReturnsFailure() {
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(new StubUserRepository());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(Guid.NewGuid(), "valid-init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenSameTelegramIdAlreadyLinked_ReturnsCurrentUser() {
        var user = User.Create("same-telegram@example.com", "secret");
        user.LinkTelegram(123456);
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(new StubUserRepository(user));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(123456, user.TelegramUserId);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenTelegramIdBelongsToAnotherUser_ReturnsFailure() {
        var user = User.Create("current-telegram@example.com", "secret");
        var existing = User.Create("existing-telegram@example.com", "secret");
        existing.LinkTelegram(123456);
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(new StubUserRepository(user, existing));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramAlreadyLinked", result.Error.Code);
    }

    [Fact]
    public async Task LinkTelegramHandler_WithAvailableTelegramId_LinksCurrentUser() {
        var user = User.Create("link@example.com", "secret");
        LinkTelegramCommandHandler handler = CreateLinkTelegramHandler(new StubUserRepository(user));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(123456, user.TelegramUserId);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WhenInitDataInvalid_ReturnsFailure() {
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator(validateFailure: true),
            new StubTelegramAssertionReplayGuard(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("bad-init-data"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator(),
            new StubTelegramAssertionReplayGuard(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("valid-init-data"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator(),
            new StubTelegramAssertionReplayGuard(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("valid-init-data"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WhenAssertionWasConsumed_RejectsReplay() {
        var user = User.Create("telegram-replay@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator(),
            new StubTelegramAssertionReplayGuard(consume: false),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramVerifyCommand("replayed-init-data"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramAssertionAlreadyUsed", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WhenWidgetDataInvalid_ReturnsFailure() {
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(),
            new StubTelegramLoginWidgetValidator(validateFailure: true),
            new StubTelegramAssertionReplayGuard(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "bad-hash", Username: null, FirstName: null, LastName: null, PhotoUrl: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(),
            new StubTelegramLoginWidgetValidator(),
            new StubTelegramAssertionReplayGuard(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "hash", Username: null, FirstName: null, LastName: null, PhotoUrl: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram-widget@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(user),
            new StubTelegramLoginWidgetValidator(),
            new StubTelegramAssertionReplayGuard(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "hash", "alex", "Alex", "User", PhotoUrl: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WhenAssertionWasConsumed_RejectsReplay() {
        var user = User.Create("telegram-widget-replay@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(user),
            new StubTelegramLoginWidgetValidator(),
            new StubTelegramAssertionReplayGuard(consume: false),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(
                123456,
                123,
                "replayed-hash",
                Username: null,
                FirstName: null,
                LastName: null,
                PhotoUrl: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramAssertionAlreadyUsed", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramBotAuthHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramBotAuthCommandHandler(
            new StubUserRepository(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramBotAuthCommand(123456), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramBotAuthHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram-bot@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramBotAuthCommandHandler(
            new StubUserRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new TelegramBotAuthCommand(123456), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }
}
