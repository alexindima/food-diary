using FoodDiary.Results;
using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

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

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserContextService {
        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? foundUser = user.Id == userId ? user : null;
            Error? error = CurrentUserAccessPolicy.EnsureCanAccess(foundUser);
            return Task.FromResult(error is not null ? Result.Failure<User>(error) : Result.Success(foundUser!));
        }

        public Task UpdateUserAsync(User userToUpdate, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
