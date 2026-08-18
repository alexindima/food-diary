using FoodDiary.Integrations.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class IntegrationOptionsTests {
    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("client-id", true)]
    public void GoogleAuthOptions_HasValidClientId_AllowsEmptyOrNonWhitespaceValues(string clientId, bool expected) {
        var options = new GoogleAuthOptions { ClientId = clientId };

        Assert.Equal(expected, GoogleAuthOptions.HasValidClientId(options));
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
    public void S3Options_HasValidMaxUploadSize_RequiresPositiveValue(long maxUploadSizeBytes, bool expected) {
        var options = new S3Options { MaxUploadSizeBytes = maxUploadSizeBytes };

        Assert.Equal(expected, S3Options.HasValidMaxUploadSize(options));
    }

    [Fact]
    public void S3Options_IsEmptyOrComplete_AcceptsEmptyConfiguration() {
        Assert.True(S3Options.IsEmptyOrComplete(new S3Options()));
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
    public void UsdaApiOptions_HasValidBaseUrl_RequiresAbsoluteHttpsUrl(string baseUrl, bool expected) {
        var options = new UsdaApiOptions { BaseUrl = baseUrl };

        Assert.Equal(expected, UsdaApiOptions.HasValidBaseUrl(options));
    }

    [Theory]
    [InlineData("https://world.openfoodfacts.org", true)]
    [InlineData("http://openfoodfacts.example.com", false)]
    [InlineData("not-a-url", false)]
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
        var invalid = new WebPushOptions {
            Enabled = true,
            Subject = "mailto:admin@example.com",
            PublicKey = "public",
            PrivateKey = "private",
            DefaultUrl = "/",
        };
        WebPushOptions missingKey = invalid.WithPublicKey("");

        Assert.True(WebPushOptions.HasValidConfiguration(invalid));
        Assert.False(WebPushOptions.HasValidConfiguration(missingKey));
    }

    [Theory]
    [InlineData("199.00", "1990.00", "RUB", "https://example.com/return", true)]
    [InlineData("0", "1990.00", "RUB", "https://example.com/return", false)]
    [InlineData("199.00", "bad", "RUB", "https://example.com/return", false)]
    [InlineData("199.00", "1990.00", "", "https://example.com/return", false)]
    [InlineData("199.00", "1990.00", "RUB", "return", false)]
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
    [InlineData("https://api.yookassa.ru/v3", true)]
    [InlineData("http://api.yookassa.test/v3", false)]
    [InlineData("not-a-url", false)]
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
