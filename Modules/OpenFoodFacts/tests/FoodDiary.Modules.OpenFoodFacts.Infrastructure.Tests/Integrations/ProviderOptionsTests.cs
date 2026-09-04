using FoodDiary.Integrations.Options;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {

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
}
