using FoodDiary.Modules.Marketing.Application.Common;

namespace FoodDiary.Modules.Marketing.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingLandingPathTests {
    [Theory]
    [InlineData("/verify-email")]
    [InlineData("/reset-password")]
    [InlineData("/auth/telegram")]
    [InlineData("/auth/telegram/callback")]
    [InlineData("/mobile/login")]
    [InlineData("/login")]
    [InlineData("/ru/login")]
    public void Sanitize_WithAuthCredentials_PreservesOnlyCampaignQuery(string path) {
        string value = path + "?token=verification-secret&userId=user&code=oauth-secret&state=oauth-state&utm_source=telegram#token=fragment-secret";

        Assert.Equal(path + "?utm_source=telegram", MarketingLandingPath.Sanitize(value));
    }

    [Fact]
    public void Sanitize_WithEncodedKeysAndNestedReturnUrl_PreservesRepeatedCampaignValues() {
        const string value = "/?%74oken=secret&%63ode=secret&returnUrl=%2Fverify-email%3Ftoken%3Dnested&utm%5Fsource=telegram&utm_term=x%2By+%26+z&utm_term=second";

        Assert.Equal("/?utm_source=telegram&utm_term=x%2By+%26+z&utm_term=second", MarketingLandingPath.Sanitize(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("javascript:secret")]
    [InlineData("http://[invalid")]
    public void Sanitize_WithInvalidInput_UsesSafeFallback(string? value) {
        Assert.Equal("/", MarketingLandingPath.Sanitize(value));
    }
}
