using FoodDiary.Domain.ValueObjects.Ids;
using System.IdentityModel.Tokens.Jwt;
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
    public void Constructor_WithoutSecretKey_Throws() {
        IOptions<JwtOptions> options = CreateOptions(includeSecret: false);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(options, new StubDateTimeProvider()));
        Assert.Contains("SecretKey", ex.Message, StringComparison.OrdinalIgnoreCase);
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

    private static IOptions<JwtOptions> CreateOptions(
        int expirationMinutes = 60,
        int refreshDays = 7,
        int rememberMeRefreshDays = 90,
        bool includeSecret = true) {
        return Microsoft.Extensions.Options.Options.Create(new JwtOptions {
            Issuer = "FoodDiary",
            Audience = "FoodDiaryClients",
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
