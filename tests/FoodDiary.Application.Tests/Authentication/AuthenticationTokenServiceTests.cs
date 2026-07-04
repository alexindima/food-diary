using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Services;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class AuthenticationTokenServiceTests {
    [Fact]
    public async Task IssueAndStoreAsync_StoresHashedRefreshToken_AndReturnsTokens() {
        User user = CreateUser("user@example.com");
        var repository = new InMemoryUserRepository(user);
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(repository, loginEvents, sessions, jwt, new StubDateTimeProvider());

        IssuedAuthenticationTokens result = await service.IssueAndStoreAsync(user, CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(
            $"sha256:{SecurityTokenGenerator.NormalizeForSecureHashing("refresh-token")}",
            Assert.Single(sessions.Items).RefreshTokenHash);
        Assert.True(repository.Updated);
        Assert.Equal(new StubDateTimeProvider().GetUtcNow().UtcDateTime, user.LastLoginAtUtc);
        Assert.Empty(loginEvents.Items);
    }

    [Fact]
    public async Task IssueAndStoreAsync_WithClientContext_RecordsLoginEvent() {
        User user = CreateUser("user@example.com");
        var repository = new InMemoryUserRepository(user);
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(repository, loginEvents, sessions, jwt, new StubDateTimeProvider());

        await service.IssueAndStoreAsync(
            user,
            CancellationToken.None,
            new AuthenticationClientContext(
                "password",
                "203.0.113.42",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36"));

        UserLoginEvent loginEvent = Assert.Single(loginEvents.Items);
        Assert.Equal(user.Id, loginEvent.UserId);
        Assert.Equal("password", loginEvent.AuthProvider);
        Assert.Equal("203.0.113.42", loginEvent.IpAddress);
        Assert.Equal("Chrome", loginEvent.BrowserName);
        Assert.Equal("Windows", loginEvent.OperatingSystem);
        Assert.Equal("Desktop", loginEvent.DeviceType);
    }

    [Fact]
    public void IssueAccessToken_UsesUserIdentityAndRoles() {
        User user = CreateUser("user@example.com", "Admin", "Support");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(
            repository,
            new InMemoryUserLoginEventRepository(),
            new InMemoryRefreshTokenSessionRepository(),
            jwt,
            new StubDateTimeProvider());

        string token = service.IssueAccessToken(user);

        Assert.Equal("access-token", token);
        Assert.Equal(user.Id, jwt.LastAccessUserId);
        Assert.Equal(user.Email, jwt.LastAccessEmail);
        Assert.Equal(["Admin", "Support"], jwt.LastAccessRoles);
        Assert.Null(jwt.LastAccessExpiresAtUtc);
    }

    [Fact]
    public void IssueAccessToken_WhenTrialIsOnlyPremiumSource_CapsAccessTokenAtTrialEnd() {
        var now = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        User user = CreateUser("trial@example.com");
        user.StartPremiumTrial(now, TimeSpan.FromDays(7));
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(
            repository,
            new InMemoryUserLoginEventRepository(),
            new InMemoryRefreshTokenSessionRepository(),
            jwt,
            new StubDateTimeProvider(now));

        _ = service.IssueAccessToken(user);

        Assert.Contains("Premium", jwt.LastAccessRoles);
        Assert.Equal(now.AddDays(7), jwt.LastAccessExpiresAtUtc);
    }

    private static User CreateUser(string email, params string[] roles) {
        var user = User.Create(email, "password-hash");
        Role[] roleEntities = [.. roles.Select(Role.Create)];
        user.ReplaceRoles(roleEntities);
        return user;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserRepository(User user) : IAuthenticationUserMutationService {
        public bool Updated { get; private set; }

        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => Task.FromResult(addedUser);
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);

        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) {
            Updated = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserLoginEventRepository : IUserLoginEventRepository {
        public List<UserLoginEvent> Items { get; } = [];

        public Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default) {
            Items.Add(loginEvent);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
            int page,
            int limit,
            Guid? userId,
            string? search,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            UserLoginEvent[] expiredItems = [.. Items
                .Where(item => item.LoggedInAtUtc < olderThanUtc)
                .OrderBy(item => item.LoggedInAtUtc)
                .Take(Math.Max(batchSize, 1))];

            foreach (UserLoginEvent? item in expiredItems) {
                Items.Remove(item);
            }

            return Task.FromResult(expiredItems.Length);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryRefreshTokenSessionRepository : IRefreshTokenSessionRepository {
        public List<UserRefreshTokenSession> Items { get; } = [];

        public Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserRefreshTokenSession?>(Items.FirstOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserRefreshTokenSession>>(
                Items.Where(item => item.UserId == userId && item.IsActive).ToList());

        public Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
            Items.Add(session);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator {
        public UserId LastAccessUserId { get; private set; }
        public string LastAccessEmail { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> LastAccessRoles { get; private set; } = [];
        public DateTime? LastAccessExpiresAtUtc { get; private set; }
        public bool LastRefreshRememberMe { get; private set; }

        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            LastAccessExpiresAtUtc = null;
            return "access-token";
        }

        public string GenerateAccessToken(
            UserId userId,
            string email,
            IReadOnlyCollection<string> roles,
            DateTime? expiresAtUtc) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            LastAccessExpiresAtUtc = expiresAtUtc;
            return "access-token";
        }

        public string GenerateAccessToken(
            UserId userId,
            string email,
            IReadOnlyCollection<string> roles,
            JwtImpersonationContext impersonation) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            return "impersonation-access-token";
        }

        public string GenerateRefreshToken(
            UserId userId,
            string email,
            IReadOnlyCollection<string> roles,
            bool rememberMe = false,
            Guid? refreshSessionId = null) {
            LastRefreshRememberMe = rememberMe;
            return "refresh-token";
        }

        public (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token) => null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider(DateTime utcNow) : TimeProvider {
        public StubDateTimeProvider()
            : this(new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)) {
        }

        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
