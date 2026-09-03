using FoodDiary.Infrastructure.Options;

namespace FoodDiary.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class JwtOptionsTests {
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
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("abcabcabcabcabcabcabcabcabcabcab")]
    public void HasValidSecretKey_WithLowCharacterDiversity_ReturnsFalse(string secretKey) {
        Assert.False(JwtOptions.HasValidSecretKey(new JwtOptions { SecretKey = secretKey }));
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
}
