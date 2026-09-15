using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Application.Commands.AcceptAiConsent;
using FoodDiary.Modules.Users.Application.Commands.RevokeAiConsent;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public class AiConsentTests {
    [Fact]
    public async Task AcceptAiConsent_WithValidUser_SetsConsentTimestamp() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task AcceptAiConsent_WhenAlreadyAccepted_RemainsIdempotent() {
        var user = User.Create("user@example.com", "hash");
        user.AcceptAiConsent();
        DateTime? originalTimestamp = user.AiConsentAcceptedAt;
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(originalTimestamp, user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task AcceptAiConsent_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new AcceptAiConsentCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task AcceptAiConsent_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task AcceptAiConsent_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new AcceptAiConsentCommandHandler(CreateAccessCheckedFailingUserContext(userId));

        Result result = await handler.Handle(new AcceptAiConsentCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task RevokeAiConsent_WithAcceptedConsent_ClearsTimestamp() {
        var user = User.Create("user@example.com", "hash");
        user.AcceptAiConsent();
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new RevokeAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task RevokeAiConsent_WhenNotAccepted_RemainsIdempotent() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new RevokeAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task RevokeAiConsent_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new RevokeAiConsentCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task RevokeAiConsent_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        Result result = await handler.Handle(new RevokeAiConsentCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task RevokeAiConsent_WhenUserLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new RevokeAiConsentCommandHandler(CreateAccessCheckedFailingUserContext(userId));

        Result result = await handler.Handle(new RevokeAiConsentCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    private static IUserContextService CreateAccessCheckedFailingUserContext(UserId userId) {
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        userContextService
            .GetAccessibleUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<User>(AuthenticationErrors.InvalidToken)));
        return userContextService;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserContextService {
        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? foundUser = user.Id == userId ? user : null;
            Error? error = CurrentUserAccessPolicy.EnsureCanAccess(foundUser);
            return Task.FromResult(error is not null ? Result.Failure<User>(error) : Result.Success(foundUser!));
        }

        public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? result.Error : null;
        }

        public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
