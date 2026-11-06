using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramStarsPublicConfigTests {
    [Theory]
    [InlineData("TelegramStars", true)]
    [InlineData(" telegramstars ", true)]
    [InlineData("TelegramStars", false)]
    public void PublicConfig_PublishesConfiguredStarsPriceOnceAndHidesDisabledCheckout(string primary, bool enabled) {
        var provider = new BillingPublicConfigProvider(Options.Create(new BillingOptions { Provider = primary }),
            Options.Create(new StripeOptions()), Options.Create(new PaddleOptions()), Options.Create(new YooKassaOptions()),
            Options.Create(new TelegramStarsOptions {
                CheckoutEnabled = enabled,
                PremiumPrice = 750,
                BotToken = "123:synthetic",
                BotApiSecret = new string('s', 32),
            }));

        BillingPublicConfigModel config = provider.GetPublicConfig();

        Assert.Multiple(() => {
            Assert.Equal(primary.Trim(), config.Provider);
            Assert.Equal(enabled ? [BillingProviderNames.TelegramStars] : Array.Empty<string>(), config.AvailableProviders);
            Assert.Equal(enabled ? 750 : (int?)null, config.TelegramStarsPrice);
            Assert.Null(config.PaddleClientToken);
        });
    }
}
