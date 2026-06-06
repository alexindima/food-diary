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
            TextModel = textModel
        };

        Assert.Equal(expectedTextModelValid, OpenAiOptions.HasTextModelWhenApiKeyConfigured(options));
        Assert.Equal(expectedVisionModelValid, OpenAiOptions.HasVisionModelWhenApiKeyConfigured(options));
    }

    [Fact]
    public void OpenAiOptions_WhenVisionModelConfigured_RequiresFallbackModel() {
        var options = new OpenAiOptions {
            VisionModel = "vision",
            VisionFallbackModel = "   "
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

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com", true)]
    [InlineData("not-a-url", false)]
    public void S3Options_HasValidPublicBaseUrl_ValidatesAbsoluteUrl(string? publicBaseUrl, bool expected) {
        var options = new S3Options { PublicBaseUrl = publicBaseUrl };

        Assert.Equal(expected, S3Options.HasValidPublicBaseUrl(options));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://s3.example.com", true)]
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
            DefaultUrl = "/"
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
            ReturnUrl = returnUrl
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
            DefaultUrl = options.DefaultUrl
        };
}
