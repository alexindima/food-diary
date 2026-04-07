using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

public class AiConsentTests {
    [Fact]
    public async Task AcceptAiConsent_WithValidUser_SetsConsentTimestamp() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task AcceptAiConsent_WhenAlreadyAccepted_RemainsIdempotent() {
        var user = User.Create("user@example.com", "hash");
        user.AcceptAiConsent();
        var originalTimestamp = user.AiConsentAcceptedAt;
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalTimestamp, user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task AcceptAiConsent_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new AcceptAiConsentCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task AcceptAiConsent_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new AcceptAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new AcceptAiConsentCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task RevokeAiConsent_WithAcceptedConsent_ClearsTimestamp() {
        var user = User.Create("user@example.com", "hash");
        user.AcceptAiConsent();
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new RevokeAiConsentCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task RevokeAiConsent_WhenNotAccepted_RemainsIdempotent() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new RevokeAiConsentCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(user.AiConsentAcceptedAt);
    }

    [Fact]
    public async Task RevokeAiConsent_WithEmptyUserId_ReturnsInvalidToken() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RevokeAiConsentCommandHandler(new SingleUserRepository(user));

        var result = await handler.Handle(new RevokeAiConsentCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Id == id ? user : null);

        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
