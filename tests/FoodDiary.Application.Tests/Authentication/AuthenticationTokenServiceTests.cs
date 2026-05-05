using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Authentication;

public class AuthenticationTokenServiceTests {
    [Fact]
    public async Task IssueAndStoreAsync_StoresHashedRefreshToken_AndReturnsTokens() {
        var user = CreateUser("user@example.com");
        var repository = new InMemoryUserRepository(user);
        var loginEvents = new InMemoryUserLoginEventRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(repository, loginEvents, jwt, new StubDateTimeProvider());

        var result = await service.IssueAndStoreAsync(user, CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(
            $"sha256:{SecurityTokenGenerator.NormalizeForSecureHashing("refresh-token")}",
            user.RefreshToken);
        Assert.True(repository.Updated);
        Assert.Empty(loginEvents.Items);
    }

    [Fact]
    public async Task IssueAndStoreAsync_WithClientContext_RecordsLoginEvent() {
        var user = CreateUser("user@example.com");
        var repository = new InMemoryUserRepository(user);
        var loginEvents = new InMemoryUserLoginEventRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(repository, loginEvents, jwt, new StubDateTimeProvider());

        await service.IssueAndStoreAsync(
            user,
            CancellationToken.None,
            new AuthenticationClientContext(
                "password",
                "203.0.113.42",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36"));

        var loginEvent = Assert.Single(loginEvents.Items);
        Assert.Equal(user.Id, loginEvent.UserId);
        Assert.Equal("password", loginEvent.AuthProvider);
        Assert.Equal("203.0.113.42", loginEvent.IpAddress);
        Assert.Equal("Chrome", loginEvent.BrowserName);
        Assert.Equal("Windows", loginEvent.OperatingSystem);
        Assert.Equal("Desktop", loginEvent.DeviceType);
    }

    [Fact]
    public void IssueAccessToken_UsesUserIdentityAndRoles() {
        var user = CreateUser("user@example.com", "Admin", "Support");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(repository, new InMemoryUserLoginEventRepository(), jwt, new StubDateTimeProvider());

        var token = service.IssueAccessToken(user);

        Assert.Equal("access-token", token);
        Assert.Equal(user.Id, jwt.LastAccessUserId);
        Assert.Equal(user.Email, jwt.LastAccessEmail);
        Assert.Equal(["Admin", "Support"], jwt.LastAccessRoles);
    }

    private static User CreateUser(string email, params string[] roles) {
        var user = User.Create(email, "password-hash");
        var roleEntities = roles.Select(Role.Create).ToArray();
        user.ReplaceRoles(roleEntities);
        return user;
    }

    private sealed class InMemoryUserRepository(User user) : IUserRepository {
        public bool Updated { get; private set; }

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

        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) {
            Updated = true;
            return Task.CompletedTask;
        }
    }

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
            var expiredItems = Items
                .Where(item => item.LoggedInAtUtc < olderThanUtc)
                .OrderBy(item => item.LoggedInAtUtc)
                .Take(Math.Max(batchSize, 1))
                .ToArray();

            foreach (var item in expiredItems) {
                Items.Remove(item);
            }

            return Task.FromResult(expiredItems.Length);
        }
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator {
        public UserId LastAccessUserId { get; private set; }
        public string LastAccessEmail { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> LastAccessRoles { get; private set; } = [];

        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
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

        public string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles) {
            return "refresh-token";
        }

        public (UserId userId, string email)? ValidateToken(string token) => null;
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
    }
}
