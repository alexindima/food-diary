using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Authentication;

public sealed class RefreshTokenCommandHandlerTests {
    [Fact]
    public async Task Handle_WithStoredRefreshToken_RotatesTokens() {
        var user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken("hashed:current-refresh-token");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, authTokenService);

        var result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(1, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithMismatchedStoredRefreshToken_ReturnsInvalidToken() {
        var user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken("hashed:other-refresh-token");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, authTokenService);

        var result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    private static User CreateUser(string email) => User.Create(email, "password-hash");

    private sealed class InMemoryUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => Task.FromResult(addedUser);
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeJwtTokenGenerator(UserId userId, string email) : IJwtTokenGenerator {
        public string GenerateAccessToken(UserId generatedUserId, string generatedEmail, IReadOnlyCollection<string> roles) => "unused-access-token";
        public string GenerateRefreshToken(UserId generatedUserId, string generatedEmail, IReadOnlyCollection<string> roles) => "unused-refresh-token";
        public (UserId userId, string email)? ValidateToken(string token) =>
            string.Equals(token, "current-refresh-token", StringComparison.Ordinal) ? (userId, email) : null;
    }

    private sealed class FakePasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hashedPassword) => string.Equals(hashedPassword, $"hashed:{password}", StringComparison.Ordinal);
    }

    private sealed class FakeAuthenticationTokenService : IAuthenticationTokenService {
        public int IssueAndStoreCallCount { get; private set; }

        public Task<IssuedAuthenticationTokens> IssueAndStoreAsync(User user, CancellationToken cancellationToken) {
            IssueAndStoreCallCount++;
            return Task.FromResult(new IssuedAuthenticationTokens("new-access-token", "new-refresh-token"));
        }

        public string IssueAccessToken(User user) => throw new NotSupportedException();
    }
}
