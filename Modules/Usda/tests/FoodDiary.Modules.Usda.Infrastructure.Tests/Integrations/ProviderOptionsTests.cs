using FoodDiary.Integrations.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {

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
}
