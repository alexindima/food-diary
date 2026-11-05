namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Options;

public sealed class TelegramStarsOptions {
    public const string SectionName = "TelegramStars";
    public bool CheckoutEnabled { get; set; }
    public int PremiumPrice { get; set; } = 500;
    public bool UseTestEnvironment { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string BotApiSecret { get; set; } = string.Empty;

    public bool CanCheckout => CheckoutEnabled && PremiumPrice is > 0 and <= 10000 &&
        !string.IsNullOrWhiteSpace(BotToken) && BotApiSecret.Length >= 16;
}
