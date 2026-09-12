using FoodDiary.Integrations.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramClientOptionsTests {
    [Theory]
    [InlineData(false, "", "", true)]
    [InlineData(true, "123", "123:test", true)]
    [InlineData(true, "124", "123:test", false)]
    [InlineData(true, "123", "", false)]
    [InlineData(true, "123", "123:", false)]
    public void OidcConfiguration_RequiresSameBot(bool enabled, string clientId, string token, bool expected) {
        var options = new TelegramOidcOptions { Enabled = enabled, ClientId = clientId };
        Assert.Equal(expected, TelegramOidcOptions.HasCompatibleBot(options, new TelegramAuthOptions { BotToken = token }));
    }

    [Theory]
    [InlineData(false, false, false, 0, "", true)]
    [InlineData(true, false, false, 0, "", false)]
    [InlineData(false, true, false, 0, "123:test", false)]
    [InlineData(true, true, false, 0, "123:test", true)]
    [InlineData(false, false, true, 123, "123:test", true)]
    [InlineData(false, false, true, 124, "123:test", false)]
    public void FeatureConfiguration_RequiresCompatibleBot(bool login, bool registration, bool operations, long botId, string token, bool expected) {
        var options = new TelegramClientOptions { LoginEnabled = login, RegistrationEnabled = registration, OperationsEnabled = operations, BotId = botId };
        Assert.Equal(expected, TelegramClientOptions.HasCompatibleBot(options, new TelegramAuthOptions { BotToken = token }));
    }
}
