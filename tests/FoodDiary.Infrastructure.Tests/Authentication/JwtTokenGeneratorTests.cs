using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Tests.Authentication;

public sealed class JwtTokenGeneratorTests {
    [Fact]
    public void GenerateAndValidateToken_RoundTrip_Succeeds() {
        var generator = new JwtTokenGenerator(CreateOptions(), new StubDateTimeProvider());
        var userId = UserId.New();
        const string email = "user@example.com";

        var token = generator.GenerateAccessToken(userId, email, ["Admin"]);
        var validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
        Assert.Equal(email, validated.Value.email);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull() {
        var generator = new JwtTokenGenerator(CreateOptions(), new StubDateTimeProvider());

        var validated = generator.ValidateToken("not-a-jwt-token");

        Assert.Null(validated);
    }

    [Fact]
    public void GenerateAccessToken_WithInvalidExpirationValues_UsesFallbacks() {
        var generator = new JwtTokenGenerator(CreateOptions(expirationMinutes: -1, refreshDays: 0), new StubDateTimeProvider());
        var userId = UserId.New();

        var token = generator.GenerateAccessToken(userId, "fallback@example.com", []);
        var validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
    }

    [Fact]
    public void Constructor_WithoutSecretKey_Throws() {
        var options = CreateOptions(includeSecret: false);

        var ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(options, new StubDateTimeProvider()));
        Assert.Contains("SecretKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IOptions<JwtOptions> CreateOptions(
        int expirationMinutes = 60,
        int refreshDays = 7,
        bool includeSecret = true) {
        return Microsoft.Extensions.Options.Options.Create(new JwtOptions {
            Issuer = "FoodDiary",
            Audience = "FoodDiaryClients",
            SecretKey = includeSecret ? "super-secret-key-for-tests-only-123456789" : string.Empty,
            ExpirationMinutes = expirationMinutes,
            RefreshTokenExpirationDays = refreshDays,
        });
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow { get; } = new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
    }
}
