using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

    [Fact]
    public async Task RegisterHandler_CreatesUserIssuesTokensAndSendsVerificationEmail() {
        var sender = new StubEmailSender();
        var tokens = new StubAuthenticationTokenService();
        var handler = new RegisterCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            tokens);

        Result<AuthenticationModel> result = await handler.Handle(
            new RegisterCommand("new@example.com", "secret", "ru", "https://client.test"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal("refresh", result.Value.RefreshToken);
        Assert.Equal("new@example.com", sender.LastEmailVerification?.ToEmail);
        Assert.Equal("https://client.test", sender.LastEmailVerification?.ClientOrigin);
        Assert.Equal("ru", tokens.LastUser?.Language);
        Assert.NotNull(tokens.LastUser?.EmailConfirmationTokenHash);
    }

    [Fact]
    public async Task RegisterHandler_WhenEmailAlreadyExists_ReturnsConflict() {
        var existingUser = User.Create("taken@example.com", "hashed");
        var repository = new StubUserRepository(existingUser);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new RegisterCommandHandler(
            repository,
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new RegisterCommand("taken@example.com", "secret", Language: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Conflict", result.Error.Code);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task RegisterHandler_WhenEmailBelongsToDeletedAccount_ReturnsAccountDeleted() {
        var deletedUser = User.Create("deleted-register@example.com", "hashed");
        deletedUser.DeleteAccount(DateTime.UtcNow);
        var repository = new StubUserRepository(deletedUser);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new RegisterCommandHandler(
            repository,
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new RegisterCommand("deleted-register@example.com", "secret", Language: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task RegisterHandler_WhenVerificationEmailEnqueueFails_Throws() {
        var handler = new RegisterCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(throwOnEmailVerification: true),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new RegisterCommand("email-failure@example.com", "secret", Language: null),
                CancellationToken.None));
    }

    [Fact]
    public async Task LoginHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-login@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand(user.Email, "secret"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task LoginHandler_WithValidCredentials_IssuesTokens() {
        var user = User.Create("login@example.com", "secret");
        var tokenService = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand(user.Email, "secret"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }
}
