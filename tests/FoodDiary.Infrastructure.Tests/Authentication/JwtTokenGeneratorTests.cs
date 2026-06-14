using FoodDiary.Domain.ValueObjects.Ids;
using System.IdentityModel.Tokens.Jwt;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class JwtTokenGeneratorTests {
    [Fact]
    public void GenerateAndValidateToken_RoundTrip_Succeeds() {
        var generator = new JwtTokenGenerator(CreateOptions(), new StubDateTimeProvider());
        var userId = UserId.New();
        const string email = "user@example.com";

        string token = generator.GenerateAccessToken(userId, email, ["Admin"]);
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
        Assert.Equal(email, validated.Value.email);
        Assert.False(validated.Value.rememberMe);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull() {
        var generator = new JwtTokenGenerator(CreateOptions(), new StubDateTimeProvider());

        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validated = generator.ValidateToken("not-a-jwt-token");

        Assert.Null(validated);
    }

    [Fact]
    public void GenerateAccessToken_WithInvalidExpirationValues_UsesFallbacks() {
        var generator = new JwtTokenGenerator(CreateOptions(expirationMinutes: -1, refreshDays: 0), new StubDateTimeProvider());
        var userId = UserId.New();

        string token = generator.GenerateAccessToken(userId, "fallback@example.com", []);
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
    }

    [Fact]
    public void GenerateRefreshToken_WithRememberMe_UsesPersistentLifetimeAndClaim() {
        var now = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var generator = new JwtTokenGenerator(CreateOptions(refreshDays: 30, rememberMeRefreshDays: 90), new StubDateTimeProvider(now));
        var userId = UserId.New();

        string token = generator.GenerateRefreshToken(userId, "remember@example.com", [], rememberMe: true);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validated = generator.ValidateToken(token);

        Assert.Equal(now.AddDays(90), jwt.ValidTo);
        Assert.NotNull(validated);
        Assert.True(validated.Value.rememberMe);
    }

    [Fact]
    public void GenerateAccessToken_WithEarlierExpirationCap_UsesCap() {
        var now = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        DateTime cap = now.AddMinutes(5);
        var generator = new JwtTokenGenerator(CreateOptions(expirationMinutes: 60), new StubDateTimeProvider(now));
        var userId = UserId.New();

        string token = generator.GenerateAccessToken(userId, "trial@example.com", ["Premium"], cap);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(cap, jwt.ValidTo);
    }

    [Fact]
    public void GenerateAccessToken_WithImpersonation_AddsImpersonationClaims() {
        var generator = new JwtTokenGenerator(CreateOptions(), new StubDateTimeProvider());
        var userId = UserId.New();
        var actorUserId = UserId.New();

        string token = generator.GenerateAccessToken(
            userId,
            "impersonated@example.com",
            ["User"],
            new JwtImpersonationContext(actorUserId, "Support request"));
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, claim =>
            string.Equals(claim.Type, JwtImpersonationClaimNames.IsImpersonation, StringComparison.Ordinal) &&
            string.Equals(claim.Value, "true", StringComparison.Ordinal));
        Assert.Contains(jwt.Claims, claim =>
            string.Equals(claim.Type, JwtImpersonationClaimNames.ActorUserId, StringComparison.Ordinal) &&
            string.Equals(claim.Value, actorUserId.Value.ToString(), StringComparison.Ordinal));
        Assert.Contains(jwt.Claims, claim =>
            string.Equals(claim.Type, JwtImpersonationClaimNames.Reason, StringComparison.Ordinal) &&
            string.Equals(claim.Value, "Support request", StringComparison.Ordinal));
    }

    [Fact]
    public void Constructor_WithoutSecretKey_Throws() {
        IOptions<JwtOptions> options = CreateOptions(includeSecret: false);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(options, new StubDateTimeProvider()));
        Assert.Contains("SecretKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithoutIssuer_Throws() {
        IOptions<JwtOptions> options = CreateOptions(issuer: " ");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(options, new StubDateTimeProvider()));

        Assert.Contains("Issuer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithoutAudience_Throws() {
        IOptions<JwtOptions> options = CreateOptions(audience: "");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(options, new StubDateTimeProvider()));

        Assert.Contains("Audience", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("change-me-via-user-secrets-or-env-32")]
    [InlineData("change-me-local-jwt-secret-min-32")]
    [InlineData("your-32-character-or-longer-secret-key")]
    public void HasValidSecretKey_WithRepositoryPlaceholder_ReturnsFalse(string secretKey) {
        Assert.False(JwtOptions.HasValidSecretKey(new JwtOptions {
            Issuer = "FoodDiary",
            Audience = "FoodDiaryClients",
            SecretKey = secretKey,
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            RememberMeRefreshTokenExpirationDays = 90,
        }));
    }

    [Theory]
    [InlineData("issuer", true)]
    [InlineData(" ", false)]
    public void HasValidIssuer_ValidatesNonWhitespace(string issuer, bool expected) {
        Assert.Equal(expected, JwtOptions.HasValidIssuer(new JwtOptions { Issuer = issuer }));
    }

    [Theory]
    [InlineData("audience", true)]
    [InlineData("", false)]
    public void HasValidAudience_ValidatesNonWhitespace(string audience, bool expected) {
        Assert.Equal(expected, JwtOptions.HasValidAudience(new JwtOptions { Audience = audience }));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void HasValidExpirationMinutes_ValidatesPositiveValue(int expirationMinutes, bool expected) {
        Assert.Equal(expected, JwtOptions.HasValidExpirationMinutes(new JwtOptions { ExpirationMinutes = expirationMinutes }));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void HasValidRefreshTokenExpirationDays_ValidatesPositiveValue(int refreshTokenExpirationDays, bool expected) {
        Assert.Equal(expected, JwtOptions.HasValidRefreshTokenExpirationDays(new JwtOptions { RefreshTokenExpirationDays = refreshTokenExpirationDays }));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void HasValidRememberMeRefreshTokenExpirationDays_ValidatesPositiveValue(int rememberMeRefreshTokenExpirationDays, bool expected) {
        Assert.Equal(
            expected,
            JwtOptions.HasValidRememberMeRefreshTokenExpirationDays(new JwtOptions {
                RememberMeRefreshTokenExpirationDays = rememberMeRefreshTokenExpirationDays,
            }));
    }

    private static IOptions<JwtOptions> CreateOptions(
        int expirationMinutes = 60,
        int refreshDays = 7,
        int rememberMeRefreshDays = 90,
        bool includeSecret = true,
        string issuer = "FoodDiary",
        string audience = "FoodDiaryClients") {
        return Microsoft.Extensions.Options.Options.Create(new JwtOptions {
            Issuer = issuer,
            Audience = audience,
            SecretKey = includeSecret ? "super-secret-key-for-tests-only-123456789" : string.Empty,
            ExpirationMinutes = expirationMinutes,
            RefreshTokenExpirationDays = refreshDays,
            RememberMeRefreshTokenExpirationDays = rememberMeRefreshDays,
        });
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider(DateTime utcNow) : TimeProvider {
        public StubDateTimeProvider()
            : this(new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc)) {
        }

        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
