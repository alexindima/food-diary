using FoodDiary.Integrations.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {

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
}
