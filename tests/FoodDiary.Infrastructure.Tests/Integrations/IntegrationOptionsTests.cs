using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using WebPush;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class IntegrationOptionsTests {
    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("client-id", true)]
    [InlineData("client id", false)]
    [InlineData("client\tid", false)]
    public void GoogleAuthOptions_HasValidClientId_RejectsWhitespaceInsideConfiguredValue(string clientId, bool expected) {
        var options = new GoogleAuthOptions { ClientId = clientId };

        Assert.Equal(expected, GoogleAuthOptions.HasValidClientId(options));
    }

    [Fact]
    public void GoogleAuthOptions_HasValidClientId_RejectsOversizedValue() {
        var options = new GoogleAuthOptions { ClientId = new string('a', 513) };

        Assert.False(GoogleAuthOptions.HasValidClientId(options));
    }

    [Theory]
    [InlineData("", "", "", true, true)]
    [InlineData("key", "vision", "", false, true)]
    [InlineData("key", "", "text", true, false)]
    [InlineData("key", "vision", "text", true, true)]
    public void OpenAiOptions_ValidationDependsOnConfiguredApiKeyAndVisionModel(
        string apiKey,
        string visionModel,
        string textModel,
        bool expectedTextModelValid,
        bool expectedVisionModelValid) {
        var options = new OpenAiOptions {
            ApiKey = apiKey,
            VisionModel = visionModel,
            VisionFallbackModel = "fallback",
            TextModel = textModel,
        };

        Assert.Equal(expectedTextModelValid, OpenAiOptions.HasTextModelWhenApiKeyConfigured(options));
        Assert.Equal(expectedVisionModelValid, OpenAiOptions.HasVisionModelWhenApiKeyConfigured(options));
    }

    [Fact]
    public void OpenAiOptions_WhenVisionModelConfigured_RequiresFallbackModel() {
        var options = new OpenAiOptions {
            VisionModel = "vision",
            VisionFallbackModel = "   ",
        };

        Assert.False(OpenAiOptions.HasVisionFallbackWhenVisionModelConfigured(options));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50 * 1024 * 1024, true)]
    [InlineData((50 * 1024 * 1024) + 1, false)]
    public void S3Options_HasValidMaxUploadSize_RequiresBoundedPositiveValue(long maxUploadSizeBytes, bool expected) {
        var options = new S3Options { MaxUploadSizeBytes = maxUploadSizeBytes };

        Assert.Equal(expected, S3Options.HasValidMaxUploadSize(options));
    }

    [Fact]
    public void S3Options_IsEmptyOrComplete_AcceptsEmptyConfiguration() {
        var options = new S3Options();

        Assert.True(S3Options.IsEmptyOrComplete(options));
        Assert.False(S3Options.HasCompleteConfiguration(options));
    }

    [Theory]
    [InlineData("access", "secret", "bucket", "eu-central-1", null, true)]
    [InlineData("access", "secret", "bucket", "", "http://minio:9000", true)]
    [InlineData("access", "", "bucket", "eu-central-1", null, false)]
    [InlineData("access", "secret", "", "eu-central-1", null, false)]
    [InlineData("access", "secret", "bucket", "", null, false)]
    public void S3Options_IsEmptyOrComplete_RejectsPartialConfiguration(
        string accessKeyId,
        string secretAccessKey,
        string bucket,
        string region,
        string? serviceUrl,
        bool expected) {
        var options = new S3Options {
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey,
            Bucket = bucket,
            Region = region,
            ServiceUrl = serviceUrl,
        };

        Assert.Equal(expected, S3Options.IsEmptyOrComplete(options));
        Assert.Equal(expected, S3Options.HasCompleteConfiguration(options));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void TelegramAuthOptions_HasValidAuthTtl_RequiresPositiveValue(int authTtlSeconds, bool expected) {
        var options = new TelegramAuthOptions { AuthTtlSeconds = authTtlSeconds };

        Assert.Equal(expected, TelegramAuthOptions.HasValidAuthTtl(options));
    }

    [Theory]
    [InlineData("https://api.nal.usda.gov/fdc/v1", true)]
    [InlineData("http://api.example.com", false)]
    [InlineData("/relative", false)]
    [InlineData("https://user:secret@api.example.com", false)]
    [InlineData("https://api.example.com?key=value", false)]
    [InlineData("https://api.example.com#fragment", false)]
    public void UsdaApiOptions_HasValidBaseUrl_RequiresAbsoluteHttpsUrl(string baseUrl, bool expected) {
        var options = new UsdaApiOptions { BaseUrl = baseUrl };

        Assert.Equal(expected, UsdaApiOptions.HasValidBaseUrl(options));
    }

    [Theory]
    [InlineData("https://world.openfoodfacts.org", true)]
    [InlineData("http://openfoodfacts.example.com", false)]
    [InlineData("not-a-url", false)]
    [InlineData("https://user:secret@openfoodfacts.example.com", false)]
    [InlineData("https://openfoodfacts.example.com?query=value", false)]
    [InlineData("https://openfoodfacts.example.com#fragment", false)]
    public void OpenFoodFactsApiOptions_HasValidBaseUrl_RequiresAbsoluteHttpsUrl(string baseUrl, bool expected) {
        var options = new OpenFoodFactsApiOptions { BaseUrl = baseUrl };

        Assert.Equal(expected, OpenFoodFactsApiOptions.HasValidBaseUrl(options));
    }

    [Theory]
    [InlineData("FoodDiary/1.0", true)]
    [InlineData("FoodDiary/1.0 (contact@example.com)", true)]
    [InlineData("", false)]
    [InlineData("FoodDiary/1.0 (", false)]
    public void OpenFoodFactsApiOptions_HasValidUserAgent_RequiresValidHttpUserAgent(string userAgent, bool expected) {
        var options = new OpenFoodFactsApiOptions { UserAgent = userAgent };

        Assert.Equal(expected, OpenFoodFactsApiOptions.HasValidUserAgent(options));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com", true)]
    [InlineData("file:///tmp/assets", false)]
    [InlineData("not-a-url", false)]
    [InlineData("https://user:secret@cdn.example.com", false)]
    [InlineData("https://cdn.example.com?query=value", false)]
    public void S3Options_HasValidPublicBaseUrl_ValidatesAbsoluteUrl(string? publicBaseUrl, bool expected) {
        var options = new S3Options { PublicBaseUrl = publicBaseUrl };

        Assert.Equal(expected, S3Options.HasValidPublicBaseUrl(options));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://s3.example.com", true)]
    [InlineData("http://minio:9000", true)]
    [InlineData("ftp://s3.example.com", false)]
    [InlineData("/relative", false)]
    [InlineData("http://user:secret@minio:9000", false)]
    [InlineData("http://minio:9000#fragment", false)]
    public void S3Options_HasValidServiceUrl_ValidatesAbsoluteUrl(string? serviceUrl, bool expected) {
        var options = new S3Options { ServiceUrl = serviceUrl };

        Assert.Equal(expected, S3Options.HasValidServiceUrl(options));
    }

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
    [InlineData("", "", "", true)]
    [InlineData("client", "secret", "https://app.example.com/fitbit", true)]
    [InlineData("client", "secret", "http://localhost:4200/fitbit", true)]
    [InlineData("client", "secret", "http://app.example.com/fitbit", false)]
    [InlineData("client", "secret", "javascript:alert(1)", false)]
    [InlineData("client", "secret", "https://user:secret@app.example.com/fitbit", false)]
    [InlineData("client", "secret", "https://app.example.com/fitbit#fragment", false)]
    public void FitbitOptions_IsEmptyOrComplete_RequiresSecureRedirectUrl(
        string clientId,
        string clientSecret,
        string redirectUri,
        bool expected) {
        var options = new FitbitOptions {
            ClientId = clientId,
            ClientSecret = clientSecret,
            RedirectUri = redirectUri,
        };

        Assert.Equal(expected, FitbitOptions.IsEmptyOrComplete(options));
        Assert.Equal(
            expected && !string.IsNullOrWhiteSpace(clientId),
            FitbitOptions.HasCompleteConfiguration(options));
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
