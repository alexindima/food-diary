using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionServiceTests {
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("missing-proof")]
    [InlineData("missing-session-id")]
    [InlineData("missing-record")]
    [InlineData("foreign-owner")]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("missing-expiry")]
    [InlineData("already-browser")]
    [InlineData("wrong-hash")]
    [InlineData("rejected-principal")]
    public async Task CreateAsync_InvalidIssuanceCannotConvertPersistOrRevokeAnySession(string fault) {
        var scenario = new Scenario(noExpiry: string.Equals(fault, "missing-expiry", StringComparison.Ordinal));
        switch (fault) {
            case "missing-proof": scenario.Tokens.ValidateToken("refresh-proof").Returns(_ => null); break;
            case "missing-session-id": scenario.Tokens.ValidateToken("refresh-proof").Returns((scenario.Session.UserId, scenario.Principal.Email, false, (RefreshTokenSessionId?)null)); break;
            case "missing-record": scenario.Sessions.GetByIdAsync(scenario.Session.Id, Arg.Any<CancellationToken>()).Returns((UserRefreshTokenSession?)null); break;
            case "foreign-owner": scenario.Tokens.ValidateToken("refresh-proof").Returns((UserId.New(), scenario.Principal.Email, false, (RefreshTokenSessionId?)scenario.Session.Id)); break;
            case "revoked": scenario.Session.Revoke(Now); break;
            case "expired": scenario.Clock.UtcNow = scenario.Session.ExpiresAtUtc!.Value; break;
            case "already-browser": scenario.Session.ConvertToBrowserSession(scenario.Session.RefreshTokenHash, 7, Now); break;
            case "wrong-hash": scenario.Session.Rotate("different-hash", rememberMe: false, Now, TimeSpan.Zero); break;
            case "rejected-principal":
                scenario.Users.GetAuthenticationPrincipalAsync(scenario.Session.UserId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<UserAuthenticationPrincipalModel>(AuthenticationErrors.InvalidToken)); break;
        }
        Result<BrowserSessionModel> result = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        ResultAssert.Failure(result, AuthenticationErrors.InvalidToken.Code);
        await scenario.Sessions.DidNotReceive().UpdateAsync(Arg.Any<UserRefreshTokenSession>(), Arg.Any<CancellationToken>());
        await scenario.Sessions.DidNotReceive().RevokeByIdAsync(Arg.Any<RefreshTokenSessionId>(), Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BrowserSessionWithoutExpiry_CannotAuthenticateOrRestore() {
        var scenario = new Scenario(noExpiry: true);
        string credential = BrowserSessionCredential.Create(scenario.Session.Id.Value);
        scenario.Session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), 7, Now);
        ResultAssert.Failure(await scenario.Service.AuthenticateAsync(credential, CancellationToken.None), AuthenticationErrors.InvalidToken.Code);
        ResultAssert.Failure(await scenario.Service.RestoreAsync(credential, CancellationToken.None), AuthenticationErrors.InvalidToken.Code);
        await scenario.Sessions.DidNotReceive().TryRevalidateBrowserSessionAsync(Arg.Any<RefreshTokenSessionId>(), Arg.Any<UserId>(),
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task CreateAsync_AlreadyExpiredAdministrativeCapCannotIssueAReplacementSession(int seconds) {
        var scenario = new Scenario();
        Result<BrowserSessionModel> result = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, Now.AddSeconds(seconds), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Null(scenario.Session.BrowserSecurityVersion);
        await scenario.Sessions.DidNotReceive().UpdateAsync(Arg.Any<UserRefreshTokenSession>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("missing-record")]
    [InlineData("wrong-secret")]
    [InlineData("legacy-token-session")]
    public async Task LogoutAsync_UnprovedCredentialCannotRevokeAnExistingSession(string fault) {
        var scenario = new Scenario();
        string credential = BrowserSessionCredential.Create(scenario.Session.Id.Value);
        if (!string.Equals(fault, "legacy-token-session", StringComparison.Ordinal)) {
            scenario.Session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), 7, Now);
        }
        switch (fault) {
            case "malformed": credential = "not-a-session-credential"; break;
            case "missing-record": scenario.Sessions.GetByIdAsync(scenario.Session.Id, Arg.Any<CancellationToken>()).Returns((UserRefreshTokenSession?)null); break;
            case "wrong-secret": credential = credential[..^1] + (credential[^1] == 'A' ? 'B' : 'A'); break;
        }
        ResultAssert.Success(await scenario.Service.LogoutAsync(credential, CancellationToken.None));
        Assert.True(scenario.Session.IsActive);
        await scenario.Sessions.DidNotReceive().RevokeByIdAsync(Arg.Any<RefreshTokenSessionId>(), Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReplacesTokenProofWithHashedOpaqueCredential() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.True(BrowserSessionCredential.TryReadSessionId(created.Value.Credential, out Guid id));
        Assert.Equal(scenario.Session.Id.Value, id);
        Assert.True(SecurityTokenGenerator.VerifyFastStorageHash(created.Value.Credential, scenario.Session.RefreshTokenHash));
        Assert.DoesNotContain(created.Value.Credential, scenario.Session.RefreshTokenHash, StringComparison.Ordinal);
        Assert.Equal(7, scenario.Session.BrowserSecurityVersion);
        Assert.True((await scenario.Service.AuthenticateAsync(created.Value.Credential, CancellationToken.None)).IsSuccess);
        Assert.True((await scenario.Service.AuthenticateAsync("refresh-proof", CancellationToken.None)).IsFailure);
        Assert.True((await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None)).IsFailure);
        Assert.Throws<InvalidOperationException>(() => scenario.Session.Rotate("hash", rememberMe: false, Now, TimeSpan.Zero));
    }

    [Fact]
    public async Task AuthenticateAsync_RejectsAlteredSecretAndRevokedSession() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        string tampered = created.Value.Credential[..^1] + (created.Value.Credential[^1] == 'A' ? 'B' : 'A');
        Assert.True((await scenario.Service.AuthenticateAsync(tampered, CancellationToken.None)).IsFailure);
        scenario.Session.Revoke(Now);
        Assert.True((await scenario.Service.AuthenticateAsync(created.Value.Credential, CancellationToken.None)).IsFailure);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task AuthenticateAsync_EnforcesExpirationBoundary(int remainingSeconds, bool allowed) {
        var scenario = new Scenario(Now.AddSeconds(remainingSeconds));
        string credential = BrowserSessionCredential.Create(scenario.Session.Id.Value);
        scenario.Session.ConvertToBrowserSession(SecurityTokenGenerator.HashForStorage(credential), 7, Now.AddMinutes(-1));
        Result<BrowserSessionModel> result = await scenario.Service.AuthenticateAsync(credential, CancellationToken.None);
        Assert.Equal(allowed, result.IsSuccess);
    }

    [Fact]
    public async Task VersionChange_RequiresRevalidation_AndCannotRestoreARevokedSession() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        scenario.Principal = scenario.Principal with { SecurityVersion = 8, Roles = ["User", "Premium"] };
        Assert.True((await scenario.Service.AuthenticateAsync(created.Value.Credential, CancellationToken.None)).IsFailure);
        Result<BrowserSessionModel> restored = await scenario.Service.RestoreAsync(created.Value.Credential, CancellationToken.None);
        Assert.True(restored.IsSuccess);
        Assert.Contains("Premium", restored.Value.Principal.Roles, StringComparer.Ordinal);
        Assert.Equal(8, scenario.Session.BrowserSecurityVersion);
        scenario.Session.Revoke(Now);
        Assert.True((await scenario.Service.RestoreAsync(created.Value.Credential, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task LogoutAsync_RevokesOnlyTheProvedSession_EvenAfterVersionChange() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        scenario.Principal = scenario.Principal with { SecurityVersion = 8 };
        await scenario.Service.LogoutAsync(created.Value.Credential, CancellationToken.None);
        await scenario.Sessions.Received(1).RevokeByIdAsync(scenario.Session.Id, scenario.Session.UserId, Now, CancellationToken.None);
        await scenario.Sessions.DidNotReceive().RevokeAllAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreAsync_RecordsActivityWithoutRotatingProofOrExtendingExpiry() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        string hash = scenario.Session.RefreshTokenHash;
        DateTime? expires = scenario.Session.ExpiresAtUtc;
        scenario.Clock.UtcNow = Now.AddHours(1);
        Assert.True((await scenario.Service.RestoreAsync(created.Value.Credential, CancellationToken.None)).IsSuccess);
        Assert.Equal(scenario.Clock.UtcNow, scenario.Session.LastRotatedAtUtc);
        Assert.Equal(hash, scenario.Session.RefreshTokenHash);
        Assert.Equal(expires, scenario.Session.ExpiresAtUtc);
    }

    [Fact]
    public async Task CreateAsync_CapsAdministrativeSessionAtTheOriginalAccessTicketLifetime() {
        var scenario = new Scenario();
        DateTime cap = Now.AddHours(1);
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, cap, CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.Equal(cap, scenario.Session.ExpiresAtUtc);
        Assert.Equal(cap, created.Value.ExpiresAtUtc);
        scenario.Clock.UtcNow = cap;
        Assert.True((await scenario.Service.RestoreAsync(created.Value.Credential, CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task RestoreAsync_WhenSessionIsRevokedBetweenProofAndRevalidation_FailsClosed() {
        var scenario = new Scenario();
        Result<BrowserSessionModel> created = await scenario.Service.CreateAsync("refresh-proof", existingCredential: null, CancellationToken.None);
        scenario.Sessions.TryRevalidateBrowserSessionAsync(scenario.Session.Id, scenario.Session.UserId,
            Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(_ => {
                scenario.Session.Revoke(Now);
                return false;
            });
        Assert.True((await scenario.Service.RestoreAsync(created.Value.Credential, CancellationToken.None)).IsFailure);
        Assert.False(scenario.Session.IsActive);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Scenario {
        public IRefreshTokenSessionWriteRepository Sessions { get; } = Substitute.For<IRefreshTokenSessionWriteRepository>();
        public UserRefreshTokenSession Session { get; }
        public UserAuthenticationPrincipalModel Principal { get; set; }
        public BrowserSessionService Service { get; }
        public IUserAuthenticationIdentityService Users { get; } = Substitute.For<IUserAuthenticationIdentityService>();
        public IJwtTokenGenerator Tokens { get; } = Substitute.For<IJwtTokenGenerator>();
        public FixedTimeProvider Clock { get; } = new();

        public Scenario(DateTime? expires = null, bool noExpiry = false) {
            var user = User.Create("browser@example.com", "test-hash");
            Principal = new UserAuthenticationPrincipalModel(user.Id, user.Email, ["User"], AccessTokenCapUtc: null, user.ToModel(), SecurityVersion: 7);
            Session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id,
                SecurityTokenGenerator.HashForStorage("refresh-proof"), rememberMe: false, authProvider: "password",
                ipAddress: null, userAgent: null, Now.AddHours(-1), noExpiry ? null : expires ?? Now.AddDays(30));
            Sessions.GetByIdAsync(Session.Id, Arg.Any<CancellationToken>()).Returns(Session);
            Sessions.TryRevalidateBrowserSessionAsync(Session.Id, user.Id, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(call => {
                    if (!Session.IsActive || Session.BrowserSecurityVersion is null ||
                        Session.BrowserSecurityVersion > call.Arg<long>() || Session.ExpiresAtUtc <= call.Arg<DateTime>() ||
                        !string.Equals(Session.RefreshTokenHash, call.Arg<string>(), StringComparison.Ordinal)) {
                        return false;
                    }
                    Session.RefreshBrowserSession(call.Arg<long>(), call.Arg<DateTime>());
                    return true;
                });
            Users.GetAuthenticationPrincipalAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(_ => Result.Success(Principal));
            Tokens.ValidateToken("refresh-proof").Returns((user.Id, user.Email, false, (RefreshTokenSessionId?)Session.Id));
            Service = new BrowserSessionService(Sessions, Users, Tokens, Clock);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public DateTime UtcNow { get; set; } = Now;
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }
}
