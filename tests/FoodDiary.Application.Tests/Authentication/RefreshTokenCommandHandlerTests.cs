using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.RefreshToken;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Results;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class RefreshTokenCommandHandlerTests {
    [Fact]
    public async Task Handle_WithStoredRefreshToken_RotatesTokens() {
        User user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken($"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(
            jwt.RefreshSessionId,
            user.Id,
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}");
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
        Assert.Equal(user.Email, result.Value.User.Email);
        Assert.Equal(1, authTokenService.IssueAndStoreCallCount);
        Assert.False(authTokenService.LastRememberMe);
    }

    [Fact]
    public async Task Handle_WithRememberMeRefreshToken_PreservesRememberMeOnRotation() {
        User user = CreateUser("remember-refresh@example.com");
        user.UpdateRefreshToken($"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("remember-refresh-token")}");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(
            jwt.RefreshSessionId,
            user.Id,
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("remember-refresh-token")}");
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("remember-refresh-token"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(authTokenService.LastRememberMe);
    }

    [Fact]
    public async Task Handle_WithMismatchedStoredRefreshToken_ReturnsInvalidToken() {
        User user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken($"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("other-refresh-token")}");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(
            jwt.RefreshSessionId,
            user.Id,
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("other-refresh-token")}");
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WhenJwtValidationFails_ReturnsInvalidToken() {
        User user = CreateUser("refresh@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("invalid-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenDoesNotContainSessionId_ReturnsInvalidToken() {
        User user = CreateUser("refresh-no-session@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("refresh-token-without-session"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsMissing_ReturnsInvalidToken() {
        User jwtUser = CreateUser("jwt@example.com");
        User repositoryUser = CreateUser("other@example.com");
        var repository = new InMemoryUserRepository(repositoryUser);
        var jwt = new FakeJwtTokenGenerator(jwtUser.Id, jwtUser.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WhenStoredRefreshTokenIsMissing_ReturnsInvalidToken() {
        User user = CreateUser("refresh@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithFastStorageHash_RotatesTokens() {
        User user = CreateUser("refresh-fast@example.com");
        user.UpdateRefreshToken(SecurityTokenGenerator.HashForStorage("current-refresh-token"));
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(
            jwt.RefreshSessionId,
            user.Id,
            SecurityTokenGenerator.HashForStorage("current-refresh-token"));
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal(1, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithPreviousRefreshTokenInsideFormerGraceWindow_ReturnsInvalidToken() {
        User user = CreateUser("refresh-previous@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var now = new DateTime(2030, 3, 28, 12, 1, 0, DateTimeKind.Utc);
        UserRefreshTokenSession session = CreateRefreshSession(
            jwt.RefreshSessionId,
            user.Id,
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}",
            now.AddMinutes(-1));
        session.Rotate(
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("rotated-refresh-token")}",
            rememberMe: false,
            now.AddSeconds(-30),
            TimeSpan.FromMinutes(2));
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(session);
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithExpiredPreviousRefreshToken_ReturnsInvalidToken() {
        User user = CreateUser("refresh-previous-expired@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var now = new DateTime(2030, 3, 28, 12, 3, 1, DateTimeKind.Utc);
        UserRefreshTokenSession session = CreateRefreshSession(
            jwt.RefreshSessionId,
            user.Id,
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}",
            now.AddMinutes(-3));
        session.Rotate(
            $"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("rotated-refresh-token")}",
            rememberMe: false,
            now.AddMinutes(-3),
            TimeSpan.FromMinutes(2));
        var refreshSessions = new InMemoryRefreshTokenSessionRepository(session);
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, refreshSessions, authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsInvalidToken() {
        User user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken($"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}");
        user.Deactivate();
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    [Fact]
    public async Task Handle_WithDeletedUser_ReturnsInvalidToken() {
        User user = CreateUser("refresh@example.com");
        user.UpdateRefreshToken($"hashed:{SecurityTokenGenerator.NormalizeForSecureHashing("current-refresh-token")}");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator(user.Id, user.Email);
        var hasher = new FakePasswordHasher();
        var authTokenService = new FakeAuthenticationTokenService();
        var handler = new RefreshTokenCommandHandler(repository, jwt, hasher, new InMemoryRefreshTokenSessionRepository(), authTokenService);

        Result<AuthenticationModel> result = await handler.Handle(new RefreshTokenCommand("current-refresh-token"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Equal(0, authTokenService.IssueAndStoreCallCount);
    }

    private static User CreateUser(string email) => User.Create(email, "password-hash");

    private static UserRefreshTokenSession CreateRefreshSession(Guid id, UserId userId, string refreshTokenHash, DateTime nowUtc) =>
        UserRefreshTokenSession.Create(
            id,
            userId,
            refreshTokenHash,
            rememberMe: false,
            authProvider: "password",
            ipAddress: null,
            userAgent: null,
            nowUtc);

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository(User user) : IUserRepository, IAuthenticationUserLookupService {
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

    [ExcludeFromCodeCoverage]
    private sealed class FakeJwtTokenGenerator(UserId userId, string email) : IJwtTokenGenerator {
        public Guid RefreshSessionId { get; } = Guid.Parse("f48a7411-0e37-4b0f-8094-c6b7c8bdb931");

        public string GenerateAccessToken(UserId generatedUserId, string generatedEmail, IReadOnlyCollection<string> roles) => "unused-access-token";
        public string GenerateAccessToken(
            UserId generatedUserId,
            string generatedEmail,
            IReadOnlyCollection<string> roles,
            DateTime? expiresAtUtc) => "unused-access-token";
        public string GenerateAccessToken(
            UserId generatedUserId,
            string generatedEmail,
            IReadOnlyCollection<string> roles,
            JwtImpersonationContext impersonation) => "unused-impersonation-access-token";
        public string GenerateRefreshToken(
            UserId generatedUserId,
            string generatedEmail,
            IReadOnlyCollection<string> roles,
            bool rememberMe = false,
            Guid? refreshSessionId = null) => "unused-refresh-token";
        public (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token) =>
            token switch {
                "current-refresh-token" => (userId, email, false, RefreshSessionId),
                "remember-refresh-token" => (userId, email, true, RefreshSessionId),
                "refresh-token-without-session" => (userId, email, false, null),
                _ => null,
            };
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryRefreshTokenSessionRepository : IRefreshTokenSessionRepository {
        private readonly List<UserRefreshTokenSession> _sessions = [];

        public InMemoryRefreshTokenSessionRepository() {
        }

        public InMemoryRefreshTokenSessionRepository(UserRefreshTokenSession session) {
            _sessions.Add(session);
        }

        public InMemoryRefreshTokenSessionRepository(Guid id, UserId userId, string refreshTokenHash) {
            _sessions.Add(CreateRefreshSession(
                id,
                userId,
                refreshTokenHash,
                new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)));
        }

        public Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserRefreshTokenSession?>(_sessions.FirstOrDefault(session => session.Id == id));

        public Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserRefreshTokenSession>>(
                _sessions.Where(session => session.UserId == userId && session.IsActive).ToList());

        public Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default) {
            foreach (UserRefreshTokenSession session in _sessions.Where(session => session.UserId == userId && session.IsActive)) {
                session.Revoke(revokedAtUtc);
            }

            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakePasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hashedPassword) => string.Equals(hashedPassword, $"hashed:{password}", StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeAuthenticationTokenService : IAuthenticationTokenService {
        public int IssueAndStoreCallCount { get; private set; }
        public bool LastRememberMe { get; private set; }

        public Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
            User user,
            CancellationToken cancellationToken,
            AuthenticationClientContext? clientContext = null,
            bool rememberMe = false,
            Guid? refreshSessionId = null) {
            IssueAndStoreCallCount++;
            LastRememberMe = rememberMe;
            return Task.FromResult(new IssuedAuthenticationTokens("new-access-token", "new-refresh-token"));
        }

        public string IssueAccessToken(User user) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime? utcNow = null) : TimeProvider {
        private readonly DateTimeOffset _utcNow = new(utcNow ?? new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc));

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
