using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using WebPush;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class IntegrationOptionsTests {

    [Fact]
    public void WebPushOptions_WhenDisabled_IsValidWithoutKeys() {
        Assert.True(WebPushOptions.HasValidConfiguration(new WebPushOptions { Enabled = false }));
    }

    [Fact]
    public void WebPushOptions_WhenEnabled_RequiresCompleteAbsoluteSubject() {
        WebPushOptions valid = CreateValidWebPushOptions();
        WebPushOptions missingKey = valid.WithPublicKey("");

        Assert.True(WebPushOptions.HasValidConfiguration(valid));
        Assert.False(WebPushOptions.HasValidConfiguration(missingKey));
    }

    [Fact]
    public void WebPushOptions_WhenEnabled_RejectsMalformedVapidKeys() {
        WebPushOptions valid = CreateValidWebPushOptions();
        WebPushOptions invalidPublicKey = valid.WithPublicKey("public");
        var invalidPrivateKey = new WebPushOptions {
            Enabled = valid.Enabled,
            Subject = valid.Subject,
            PublicKey = valid.PublicKey,
            PrivateKey = "private",
            DefaultUrl = valid.DefaultUrl,
        };

        Assert.False(WebPushOptions.HasValidConfiguration(invalidPublicKey));
        Assert.False(WebPushOptions.HasValidConfiguration(invalidPrivateKey));
    }

    [Fact]
    public void WebPushOptions_WhenEnabled_RejectsMismatchedVapidKeyPair() {
        VapidDetails firstPair = VapidHelper.GenerateVapidKeys();
        VapidDetails secondPair = VapidHelper.GenerateVapidKeys();
        var options = new WebPushOptions {
            Enabled = true,
            Subject = "mailto:admin@example.com",
            PublicKey = firstPair.PublicKey,
            PrivateKey = secondPair.PrivateKey,
            DefaultUrl = "/",
        };

        Assert.False(WebPushOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void WebPushOptions_WhenEnabledAndDefaultUrlIsNull_ReturnsFalse() {
        WebPushOptions valid = CreateValidWebPushOptions();
        var options = new WebPushOptions {
            Enabled = valid.Enabled,
            Subject = valid.Subject,
            PublicKey = valid.PublicKey,
            PrivateKey = valid.PrivateKey,
            DefaultUrl = null!,
        };

        Assert.False(WebPushOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("mailto:admin@example.com", true)]
    [InlineData("mailto:not-an-address", false)]
    [InlineData("mailto:admin@example.com#fragment", false)]
    [InlineData("https://example.com/contact", true)]
    [InlineData("http://example.com/contact", false)]
    [InlineData("file:///tmp/contact", false)]
    [InlineData("javascript:alert(1)", false)]
    public void WebPushOptions_WhenEnabled_ValidatesVapidSubject(string subject, bool expected) {
        WebPushOptions options = CreateValidWebPushOptions(subject: subject);

        Assert.Equal(expected, WebPushOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/notifications", true)]
    [InlineData("/\\attacker.example/notifications", false)]
    [InlineData("/notifications\nignored", false)]
    [InlineData("https://app.example.com/notifications", true)]
    [InlineData("//attacker.example/notifications", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://user:secret@app.example.com", false)]
    [InlineData("relative/path", false)]
    public void WebPushOptions_WhenEnabled_ValidatesDefaultNavigationUrl(string defaultUrl, bool expected) {
        WebPushOptions options = CreateValidWebPushOptions(defaultUrl: defaultUrl);

        Assert.Equal(expected, WebPushOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void StripeOptions_HasValidConfiguration_DoesNotRequireUnusedPublishableKey() {
        var options = new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            PortalReturnUrl = "https://example.com/portal",
        };

        Assert.True(StripeOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void StripeOptions_HasAnyConfiguration_IgnoresUnusedPublishableKey() {
        var options = new StripeOptions { PublishableKey = "pk_legacy" };

        Assert.False(StripeOptions.HasAnyConfiguration(options));
    }

    private static WebPushOptions CreateValidWebPushOptions(
        string subject = "mailto:admin@example.com",
        string defaultUrl = "/") {
        VapidDetails keys = VapidHelper.GenerateVapidKeys();
        return new WebPushOptions {
            Enabled = true,
            Subject = subject,
            PublicKey = keys.PublicKey,
            PrivateKey = keys.PrivateKey,
            DefaultUrl = defaultUrl,
        };
    }

    [Theory]
    [InlineData("199.00", "1990.00", "RUB", "https://example.com/return", true)]
    [InlineData("0", "1990.00", "RUB", "https://example.com/return", false)]
    [InlineData("199.00", "bad", "RUB", "https://example.com/return", false)]
    [InlineData("199.00", "1990.00", "", "https://example.com/return", false)]
    [InlineData("199.00", "1990.00", "RUB", "return", false)]
    [InlineData("199.00", "1990.00", "RUB", "http://example.com/return", false)]
    [InlineData("199.00", "1990.00", "RUB", "javascript:alert(1)", false)]
    public void YooKassaOptions_HasValidCheckoutConfiguration_ValidatesRequiredCheckoutFields(
        string monthlyAmount,
        string yearlyAmount,
        string currency,
        string returnUrl,
        bool expected) {
        var options = new YooKassaOptions {
            ShopId = "shop",
            SecretKey = "secret",
            PremiumMonthlyAmount = monthlyAmount,
            PremiumYearlyAmount = yearlyAmount,
            Currency = currency,
            ReturnUrl = returnUrl,
        };

        Assert.Equal(expected, YooKassaOptions.HasValidCheckoutConfiguration(options));
    }

    [Theory]
    [InlineData("https://checkout.example/path", true)]
    [InlineData("http://checkout.example/path", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/relative", false)]
    [InlineData("https://user:password@checkout.example/path", false)]
    public void BillingUrlValidator_IsAbsoluteHttps_RejectsUnsafeNavigationUrls(string url, bool expected) {
        Assert.Equal(expected, BillingUrlValidator.IsAbsoluteHttps(url));
    }

    [Theory]
    [InlineData("https://api.yookassa.ru/v3", true)]
    [InlineData("http://api.yookassa.test/v3", false)]
    [InlineData("not-a-url", false)]
    [InlineData("https://user:secret@api.yookassa.test/v3", false)]
    [InlineData("https://api.yookassa.test/v3?query=value", false)]
    [InlineData("https://api.yookassa.test/v3#fragment", false)]
    public void YooKassaOptions_HasValidCheckoutConfiguration_RequiresHttpsApiBaseUrl(string apiBaseUrl, bool expected) {
        var options = new YooKassaOptions {
            ShopId = "shop",
            SecretKey = "secret",
            ApiBaseUrl = apiBaseUrl,
            PremiumMonthlyAmount = "199.00",
            PremiumYearlyAmount = "1990.00",
            Currency = "RUB",
            ReturnUrl = "https://example.com/return",
        };

        Assert.Equal(expected, YooKassaOptions.HasValidCheckoutConfiguration(options));
    }
}

[ExcludeFromCodeCoverage]
file static class WebPushOptionsTestExtensions {
    public static WebPushOptions WithPublicKey(this WebPushOptions options, string publicKey) =>
        new() {
            Enabled = options.Enabled,
            Subject = options.Subject,
            PublicKey = publicKey,
            PrivateKey = options.PrivateKey,
            DefaultUrl = options.DefaultUrl,
        };
}
