using FoodDiary.Integrations.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {
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
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void TelegramAuthOptions_HasValidAuthTtl_RequiresPositiveValue(int authTtlSeconds, bool expected) {
        var options = new TelegramAuthOptions { AuthTtlSeconds = authTtlSeconds };

        Assert.Equal(expected, TelegramAuthOptions.HasValidAuthTtl(options));
    }
}
