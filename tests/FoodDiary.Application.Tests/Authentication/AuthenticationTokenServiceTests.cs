using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Authentication;

public class AuthenticationTokenServiceTests {
    [Fact]
    public async Task IssueAndStoreAsync_StoresHashedRefreshToken_AndReturnsTokens() {
        var user = CreateUser("user@example.com");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator();
        var hasher = new FakePasswordHasher();
        var service = new AuthenticationTokenService(repository, jwt, hasher);

        var result = await service.IssueAndStoreAsync(user, CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal("hashed:refresh-token", user.RefreshToken);
        Assert.True(repository.Updated);
    }

    [Fact]
    public void IssueAccessToken_UsesUserIdentityAndRoles() {
        var user = CreateUser("user@example.com", "Admin", "Support");
        var repository = new InMemoryUserRepository(user);
        var jwt = new FakeJwtTokenGenerator();
        var hasher = new FakePasswordHasher();
        var service = new AuthenticationTokenService(repository, jwt, hasher);

        var token = service.IssueAccessToken(user);

        Assert.Equal("access-token", token);
        Assert.Equal(user.Id, jwt.LastAccessUserId);
        Assert.Equal(user.Email, jwt.LastAccessEmail);
        Assert.Equal(["Admin", "Support"], jwt.LastAccessRoles);
    }

    private static User CreateUser(string email, params string[] roles) {
        var user = User.Create(email, "password-hash");
        foreach (var roleName in roles) {
            var role = Role.Create(roleName);
            var userRole = new UserRole(user.Id, role.Id);
            user.UserRoles.Add(userRole);
            role.UserRoles.Add(userRole);
            SetNavigation(userRole, user, role);
        }

        return user;
    }

    private static void SetNavigation(UserRole userRole, User user, Role role) {
        typeof(UserRole).GetProperty(nameof(UserRole.User))!.SetValue(userRole, user);
        typeof(UserRole).GetProperty(nameof(UserRole.Role))!.SetValue(userRole, role);
    }

    private sealed class InMemoryUserRepository(User user) : IUserRepository {
        public bool Updated { get; private set; }

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmailIncludingDeletedAsync(string email) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdAsync(UserId id) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId) => Task.FromResult<User?>(null);
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId) => Task.FromResult<User?>(null);
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted) =>
            throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names) =>
            throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser) => Task.FromResult(addedUser);

        public Task UpdateAsync(User updatedUser) {
            Updated = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hashedPassword) => hashedPassword == $"hashed:{password}";
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

        public string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles) {
            return "refresh-token";
        }

        public (UserId userId, string email)? ValidateToken(string token) => null;
    }
}
