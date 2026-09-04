using FoodDiary.Integrations.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {

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
}
