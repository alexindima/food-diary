using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Infrastructure.Tests.Authentication;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateAndValidateToken_RoundTrip_Succeeds()
    {
        var generator = new JwtTokenGenerator(CreateConfiguration());
        var userId = UserId.New();
        const string email = "user@example.com";

        var token = generator.GenerateAccessToken(userId, email, ["Admin"]);
        var validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
        Assert.Equal(email, validated.Value.email);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        var generator = new JwtTokenGenerator(CreateConfiguration());

        var validated = generator.ValidateToken("not-a-jwt-token");

        Assert.Null(validated);
    }

    [Fact]
    public void GenerateAccessToken_WithInvalidExpirationValues_UsesFallbacks()
    {
        var generator = new JwtTokenGenerator(CreateConfiguration(expirationMinutes: "-1", refreshDays: "abc"));
        var userId = UserId.New();

        var token = generator.GenerateAccessToken(userId, "fallback@example.com", []);
        var validated = generator.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(userId, validated.Value.userId);
    }

    [Fact]
    public void Constructor_WithoutSecretKey_Throws()
    {
        var config = CreateConfiguration(includeSecret: false);

        var ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(config));
        Assert.Contains("SecretKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration CreateConfiguration(
        string expirationMinutes = "60",
        string refreshDays = "7",
        bool includeSecret = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "FoodDiary",
            ["JwtSettings:Audience"] = "FoodDiaryClients",
            ["JwtSettings:ExpirationMinutes"] = expirationMinutes,
            ["JwtSettings:RefreshTokenExpirationDays"] = refreshDays,
        };

        if (includeSecret)
        {
            values["JwtSettings:SecretKey"] = "super-secret-key-for-tests-only-123456789";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
