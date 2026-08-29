using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Mappings;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class AuthenticationTokenServiceTests {
    [Fact]
    public async Task IssueFromPrincipalAsync_StoresSessionWithoutMutatingUserAggregate() {
        User user = CreateUser("principal@example.com");
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(loginEvents, sessions, jwt, new StubDateTimeProvider());
        var principal = new UserAuthenticationPrincipalModel(
            user.Id,
            user.Email,
            ["User"],
            AccessTokenCapUtc: null,
            user.ToModel());

        IssuedAuthenticationTokens result = await service
            .IssueFromPrincipalAsync(principal, CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        UserRefreshTokenSession session = Assert.Single(sessions.Items);
        Assert.Equal(user.Id, session.UserId);
        Assert.Empty(loginEvents.Items);
        Assert.Equal(["User"], jwt.LastAccessRoles);
        Assert.Equal(session.Id, jwt.LastAccessRefreshSessionId);
        Assert.Equal(session.Id, jwt.LastRefreshSessionId);
    }

    [Fact]
    public async Task IssueFromPrincipalAsync_WithClientContextAndRememberMe_RecordsLoginEvent() {
        User user = CreateUser("principal-context@example.com");
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(loginEvents, sessions, jwt, new StubDateTimeProvider());
        var principal = new UserAuthenticationPrincipalModel(
            user.Id,
            user.Email,
            ["User"],
            AccessTokenCapUtc: null,
            user.ToModel());
        var clientContext = new AuthenticationClientContext(
            "password",
            "203.0.113.42",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/125.0.0.0 Safari/537.36");

        await service.IssueFromPrincipalAsync(
            principal,
            CancellationToken.None,
            clientContext,
            rememberMe: true);

        UserRefreshTokenSession session = Assert.Single(sessions.Items);
        Assert.True(session.RememberMe);
        UserLoginEvent loginEvent = Assert.Single(loginEvents.Items);
        Assert.Equal(user.Id, loginEvent.UserId);
        Assert.Equal("password", loginEvent.AuthProvider);
        Assert.Equal("203.0.113.42", loginEvent.IpAddress);
        Assert.True(jwt.LastRefreshRememberMe);
    }

    [Fact]
    public async Task IssueFromPrincipalAsync_WithExistingSession_RotatesWithoutAddingSession() {
        User user = CreateUser("principal-rotation@example.com");
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var dateTimeProvider = new StubDateTimeProvider();
        var service = new AuthenticationTokenService(
            new InMemoryUserLoginEventRepository(),
            sessions,
            jwt,
            dateTimeProvider);
        var principal = new UserAuthenticationPrincipalModel(
            user.Id,
            user.Email,
            ["User"],
            AccessTokenCapUtc: null,
            user.ToModel());
        var refreshSessionId = Guid.Parse("3f0a9db0-72a7-4ce1-a149-395bf13ba8bc");
        var existingSession = UserRefreshTokenSession.Create(
            refreshSessionId,
            user.Id,
            "old-refresh-hash",
            rememberMe: false,
            authProvider: "password",
            ipAddress: null,
            userAgent: null,
            dateTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1));
        await sessions.AddAsync(existingSession, CancellationToken.None);

        IssuedAuthenticationTokens? tokens = await service.RotateFromPrincipalAsync(
            principal,
            refreshSessionId,
            "old-refresh-hash",
            rememberMe: true,
            CancellationToken.None);

        Assert.NotNull(tokens);
        UserRefreshTokenSession rotatedSession = Assert.Single(sessions.Items);
        Assert.Same(existingSession, rotatedSession);
        Assert.Equal(
            $"sha256:{SecurityTokenGenerator.NormalizeForSecureHashing("refresh-token")}",
            rotatedSession.RefreshTokenHash);
        Assert.True(rotatedSession.RememberMe);
        Assert.Equal(refreshSessionId, jwt.LastAccessRefreshSessionId);
        Assert.Equal(refreshSessionId, jwt.LastRefreshSessionId);
    }

    private static User CreateUser(string email, params string[] roles) {
        var user = User.Create(email, "password-hash");
        Role[] roleEntities = [.. roles.Select(Role.Create)];
        user.ReplaceRoles(roleEntities);
        return user;
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

        public Task<bool> TryRotateAsync(
            Guid id,
            UserId userId,
            string expectedRefreshTokenHash,
            string newRefreshTokenHash,
            bool rememberMe,
            DateTime rotatedAtUtc,
            CancellationToken cancellationToken = default) {
            UserRefreshTokenSession? session = Items.FirstOrDefault(item => item.Id == id);
            if (session is null || session.UserId != userId || !session.IsActive ||
                !string.Equals(session.RefreshTokenHash, expectedRefreshTokenHash, StringComparison.Ordinal)) {
                return Task.FromResult(result: false);
            }

            session.Rotate(newRefreshTokenHash, rememberMe, rotatedAtUtc, TimeSpan.Zero);
            return Task.FromResult(result: true);
        }

        public Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default) {
            foreach (UserRefreshTokenSession session in Items.Where(session => session.UserId == userId && session.IsActive)) {
                session.Revoke(revokedAtUtc);
            }

            return Task.CompletedTask;
        }

        public Task RevokeByIdAsync(
            Guid id,
            UserId userId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default) {
            Items.FirstOrDefault(session => session.Id == id && session.UserId == userId)?.Revoke(revokedAtUtc);
            return Task.CompletedTask;
        }

        public Task RevokeOtherByIdAsync(
            Guid id,
            UserId userId,
            Guid currentSessionId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default) {
            bool currentIsActive = Items.Any(session =>
                session.Id == currentSessionId && session.UserId == userId && session.IsActive);
            if (currentIsActive) {
                Items.FirstOrDefault(session =>
                    session.Id == id && session.Id != currentSessionId && session.UserId == userId)?.Revoke(revokedAtUtc);
            }
            return Task.CompletedTask;
        }

        public Task RevokeAllOtherAsync(
            UserId userId,
            Guid currentSessionId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default) {
            bool currentIsActive = Items.Any(session =>
                session.Id == currentSessionId && session.UserId == userId && session.IsActive);
            if (currentIsActive) {
                foreach (UserRefreshTokenSession session in Items.Where(session =>
                    session.UserId == userId && session.Id != currentSessionId && session.IsActive)) {
                    session.Revoke(revokedAtUtc);
                }
            }
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator {
        public UserId LastAccessUserId { get; private set; }
        public string LastAccessEmail { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> LastAccessRoles { get; private set; } = [];
        public DateTime? LastAccessExpiresAtUtc { get; private set; }
        public Guid? LastAccessRefreshSessionId { get; private set; }
        public bool LastRefreshRememberMe { get; private set; }
        public Guid? LastRefreshSessionId { get; private set; }

        public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles, long securityVersion = 0) {
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
            DateTime? expiresAtUtc,
            long securityVersion = 0) {
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
            DateTime? expiresAtUtc,
            long securityVersion,
            Guid refreshSessionId) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            LastAccessExpiresAtUtc = expiresAtUtc;
            LastAccessRefreshSessionId = refreshSessionId;
            return "access-token";
        }

        public string GenerateAccessToken(
            UserId userId,
            string email,
            IReadOnlyCollection<string> roles,
            JwtImpersonationContext impersonation,
            long securityVersion = 0) {
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
            LastRefreshSessionId = refreshSessionId;
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
