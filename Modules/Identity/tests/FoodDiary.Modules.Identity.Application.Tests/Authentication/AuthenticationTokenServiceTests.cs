using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Authentication.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public class AuthenticationTokenServiceTests {
    private static readonly IAuthenticationSessionLifetimePolicy Lifetimes = CreateLifetimePolicy();

    private static IAuthenticationSessionLifetimePolicy CreateLifetimePolicy() {
        IAuthenticationSessionLifetimePolicy policy = Substitute.For<IAuthenticationSessionLifetimePolicy>();
        policy.GetLifetime(AuthenticationCredentialKind.Browser, rememberMe: false).Returns(TimeSpan.FromDays(30));
        policy.GetLifetime(AuthenticationCredentialKind.Browser, rememberMe: true).Returns(TimeSpan.FromDays(90));
        policy.GetLifetime(AuthenticationCredentialKind.AdminBrowser, Arg.Any<bool>()).Returns(TimeSpan.FromHours(1));
        return policy;
    }

    [Theory]
    [InlineData(AuthenticationCredentialKind.Browser, false, 43200)]
    [InlineData(AuthenticationCredentialKind.Browser, true, 129600)]
    [InlineData(AuthenticationCredentialKind.AdminBrowser, false, 5)]
    public async Task BrowserIssuance_CreatesOnlyHashedOpaqueProofAndPreservesLifetimePolicy(AuthenticationCredentialKind kind, bool remember, int lifetimeMinutes) {
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var loginEvents = new InMemoryUserLoginEventRepository();
        IJwtTokenGenerator jwt = Substitute.For<IJwtTokenGenerator>();
        var service = new AuthenticationTokenService(loginEvents, sessions, jwt, Substitute.For<IPasswordHasher>(), clock, Lifetimes);
        var principal = new UserAuthenticationPrincipalModel(UserId.New(), "opaque@example.com", ["User", "Premium"], now.AddMinutes(5), User: null!, SecurityVersion: 7);
        IssuedAuthenticationTokens issued = await service.IssueFromPrincipalAsync(principal, CancellationToken.None,
            new AuthenticationClientContext("password", IpAddress: null, UserAgent: null, CredentialKind: kind), remember);
        IssuedBrowserSession browser = Assert.IsType<IssuedBrowserSession>(issued.BrowserSession);
        UserRefreshTokenSession session = Assert.Single(sessions.Items);
        Assert.Equal(kind == AuthenticationCredentialKind.Browser, session.BrowserSlidingExpirationEnabled);
        Assert.Empty(jwt.ReceivedCalls());
        Assert.Empty(issued.AccessToken);
        Assert.Empty(issued.RefreshToken);
        Assert.True(SecurityTokenGenerator.VerifyFastStorageHash(browser.Credential, session.RefreshTokenHash));
        Assert.DoesNotContain(browser.Credential, browser.ToString(), StringComparison.Ordinal);
        Assert.Equal(session.Id.Value, browser.SessionId);
        Assert.Equal(7, session.BrowserSecurityVersion);
        Assert.Equal(now.AddMinutes(lifetimeMinutes), session.ExpiresAtUtc);
        Assert.Equal(remember, session.RememberMe);
        Assert.Single(loginEvents.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrowserIssuance_ReplacesOnlyThePreviousProvenCookieEvenWhenItBelongsToAnotherAccount(bool validProof) {
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var id = RefreshTokenSessionId.New();
        string previousCredential = BrowserSessionCredential.Create(id.Value);
        var previous = UserRefreshTokenSession.Create(id, UserId.New(), SecurityTokenGenerator.HashForStorage(previousCredential),
            rememberMe: false, "password", ipAddress: null, userAgent: null, now.AddDays(-1), now.AddDays(1));
        previous.ConvertToBrowserSession(previous.RefreshTokenHash, 7, now.AddDays(-1));
        sessions.Items.Add(previous);
        var principal = new UserAuthenticationPrincipalModel(UserId.New(), "new-account@example.com", ["User"], AccessTokenCapUtc: null, User: null!);
        var service = new AuthenticationTokenService(new InMemoryUserLoginEventRepository(), sessions, Substitute.For<IJwtTokenGenerator>(),
            Substitute.For<IPasswordHasher>(), clock, Lifetimes);
        string proof = validProof ? previousCredential : BrowserSessionCredential.Create(id.Value);
        IssuedAuthenticationTokens issued = await service.IssueFromPrincipalAsync(principal, CancellationToken.None,
            new AuthenticationClientContext("password", IpAddress: null, UserAgent: null, CredentialKind: AuthenticationCredentialKind.Browser,
                ExistingBrowserCredential: proof));
        Assert.Equal(validProof, previous.RevokedAtUtc.HasValue);
        Assert.NotEqual(id.Value, issued.BrowserSession!.SessionId);
        Assert.Equal(2, sessions.Items.Count);
        Assert.True(sessions.Items.Single(session => session.Id != id).IsActive);
    }
    [Fact]
    public async Task IssueFromPrincipalAsync_StoresSessionWithoutMutatingUserAggregate() {
        User user = CreateUser("principal@example.com");
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(loginEvents, sessions, jwt, Substitute.For<IPasswordHasher>(), new StubDateTimeProvider(), Lifetimes);
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
        Assert.Equal(jwt.ReadRefreshTokenExpirationUtc(result.RefreshToken), session.ExpiresAtUtc);
    }

    [Fact]
    public async Task IssueFromPrincipalAsync_WithClientContextAndRememberMe_RecordsLoginEvent() {
        User user = CreateUser("principal-context@example.com");
        var loginEvents = new InMemoryUserLoginEventRepository();
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var jwt = new FakeJwtTokenGenerator();
        var service = new AuthenticationTokenService(loginEvents, sessions, jwt, Substitute.For<IPasswordHasher>(), new StubDateTimeProvider(), Lifetimes);
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
            Substitute.For<IPasswordHasher>(),
            dateTimeProvider, Lifetimes);
        var principal = new UserAuthenticationPrincipalModel(
            user.Id,
            user.Email,
            ["User"],
            AccessTokenCapUtc: null,
            user.ToModel());
        var refreshSessionId = new RefreshTokenSessionId(Guid.Parse("3f0a9db0-72a7-4ce1-a149-395bf13ba8bc"));
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
        Assert.Equal(jwt.ReadRefreshTokenExpirationUtc(tokens.RefreshToken), rotatedSession.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("invalid", false)]
    [InlineData("foreign-user", false)]
    [InlineData("foreign-session", false)]
    [InlineData("wrong-hash", false)]
    [InlineData("revoked", false)]
    [InlineData("missing-session", false)]
    public async Task RepeatLogin_ReplacesOnlyTheSessionProvenByTheSameUserCookie(string cookieState, bool replaces) {
        User user = CreateUser("repeat-login@example.com");
        var clock = new StubDateTimeProvider();
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var sessions = new InMemoryRefreshTokenSessionRepository();
        var previous = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(),
            cookieState is "foreign-session" ? UserId.New() : user.Id,
            SecurityTokenGenerator.HashForStorage(cookieState is "wrong-hash" ? "different-token" : "existing-refresh-token"),
            rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "same-browser", now.AddDays(-1));
        var otherBrowser = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "other-hash",
            rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "same-browser", now.AddDays(-1));
        if (cookieState is "revoked") {
            previous.Revoke(now.AddHours(-1));
        }
        await sessions.AddAsync(previous);
        await sessions.AddAsync(otherBrowser);
        var jwt = new FakeJwtTokenGenerator {
            ExistingTokenIdentity = cookieState is "invalid" ? null :
                (cookieState is "foreign-user" ? UserId.New() : user.Id, user.Email, false,
                    cookieState is "missing-session" ? RefreshTokenSessionId.New() : previous.Id),
        };
        var service = new AuthenticationTokenService(new InMemoryUserLoginEventRepository(), sessions, jwt,
            Substitute.For<IPasswordHasher>(), clock, Lifetimes);
        var principal = new UserAuthenticationPrincipalModel(user.Id, user.Email, ["User"], AccessTokenCapUtc: null, user.ToModel());

        await service.IssueFromPrincipalAsync(principal, CancellationToken.None,
            new AuthenticationClientContext("password", IpAddress: null, "same-browser", "existing-refresh-token"));

        UserRefreshTokenSession replacement = sessions.Items.Single(session => session.Id != previous.Id && session.Id != otherBrowser.Id);
        Assert.Multiple(
            () => Assert.Equal(replaces || cookieState is "revoked", previous.RevokedAtUtc.HasValue),
            () => Assert.True(otherBrowser.IsActive),
            () => Assert.True(replacement.IsActive),
            () => Assert.Equal(user.Id, replacement.UserId),
            () => Assert.Equal(3, sessions.Items.Count));
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
            CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? provider = null, string? device = null) =>
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
        public Task<BrowserSessionProof?> ReadBrowserSessionProofAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> TryRenewBrowserSessionAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRecordBrowserActivityAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime lastActiveBeforeUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> DeleteInactiveBeforeAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public List<UserRefreshTokenSession> Items { get; } = [];

        public Task<UserRefreshTokenSession?> GetByIdAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default) =>
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

        public Task<bool> TryRevalidateBrowserSessionAsync(RefreshTokenSessionId id, UserId userId, string expectedCredentialHash,
            long securityVersion, DateTime nowUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This fixture exercises refresh token issuance only.");

        public Task<bool> TryRotateAsync(
            RefreshTokenSessionId id,
            UserId userId,
            string expectedRefreshTokenHash,
            string newRefreshTokenHash,
            bool rememberMe,
            DateTime rotatedAtUtc,
            DateTime? expiresAtUtc = null,
            CancellationToken cancellationToken = default) {
            UserRefreshTokenSession? session = Items.FirstOrDefault(item => item.Id == id);
            if (session is null || session.UserId != userId || !session.IsActive ||
                !string.Equals(session.RefreshTokenHash, expectedRefreshTokenHash, StringComparison.Ordinal)) {
                return Task.FromResult(result: false);
            }

            session.Rotate(newRefreshTokenHash, rememberMe, rotatedAtUtc, TimeSpan.Zero, expiresAtUtc);
            return Task.FromResult(result: true);
        }

        public Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default) {
            foreach (UserRefreshTokenSession session in Items.Where(session => session.UserId == userId && session.IsActive)) {
                session.Revoke(revokedAtUtc);
            }

            return Task.CompletedTask;
        }

        public Task RevokeByIdAsync(
            RefreshTokenSessionId id,
            UserId userId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken = default) {
            Items.FirstOrDefault(session => session.Id == id && session.UserId == userId)?.Revoke(revokedAtUtc);
            return Task.CompletedTask;
        }

        public Task RevokeOtherByIdAsync(
            RefreshTokenSessionId id,
            UserId userId,
            RefreshTokenSessionId currentSessionId,
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
            RefreshTokenSessionId currentSessionId,
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
        public (UserId userId, string? email, bool rememberMe, RefreshTokenSessionId? refreshSessionId)? ExistingTokenIdentity { get; init; }
        public UserId LastAccessUserId { get; private set; }
        public string? LastAccessEmail { get; private set; }
        public IReadOnlyCollection<string> LastAccessRoles { get; private set; } = [];
        public DateTime? LastAccessExpiresAtUtc { get; private set; }
        public RefreshTokenSessionId? LastAccessRefreshSessionId { get; private set; }
        public bool LastRefreshRememberMe { get; private set; }
        public RefreshTokenSessionId? LastRefreshSessionId { get; private set; }

        public string GenerateAccessToken(UserId userId, string? email, IReadOnlyCollection<string> roles, long securityVersion = 0) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            LastAccessExpiresAtUtc = null;
            return "access-token";
        }

        public string GenerateAccessToken(
            UserId userId,
            string? email,
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
            string? email,
            IReadOnlyCollection<string> roles,
            DateTime? expiresAtUtc,
            long securityVersion,
            RefreshTokenSessionId refreshSessionId) {
            LastAccessUserId = userId;
            LastAccessEmail = email;
            LastAccessRoles = roles.ToArray();
            LastAccessExpiresAtUtc = expiresAtUtc;
            LastAccessRefreshSessionId = refreshSessionId;
            return "access-token";
        }

        public string GenerateAccessToken(
            UserId userId,
            string? email,
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
            string? email,
            IReadOnlyCollection<string> roles,
            bool rememberMe = false,
            RefreshTokenSessionId? refreshSessionId = null) {
            LastRefreshRememberMe = rememberMe;
            LastRefreshSessionId = refreshSessionId;
            return "refresh-token";
        }

        public DateTime ReadRefreshTokenExpirationUtc(string refreshToken) => new(2030, 6, 28, 12, 0, 0, DateTimeKind.Utc);

        public (UserId userId, string? email, bool rememberMe, RefreshTokenSessionId? refreshSessionId)? ValidateToken(string token) =>
            token is "existing-refresh-token" ? ExistingTokenIdentity : null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider(DateTime utcNow) : TimeProvider {
        public StubDateTimeProvider()
            : this(new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)) {
        }

        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
