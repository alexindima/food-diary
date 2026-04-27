namespace FoodDiary.Integrations.Options;

public sealed class YooKassaOptions {
    public const string SectionName = "YooKassa";

    public string ShopId { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.yookassa.ru/v3";
    public string PremiumMonthlyAmount { get; init; } = string.Empty;
    public string PremiumYearlyAmount { get; init; } = string.Empty;
    public string Currency { get; init; } = "RUB";
    public string ReturnUrl { get; init; } = string.Empty;
    public string Description { get; init; } = "FoodDiary Premium";

    public static bool HasValidCheckoutConfiguration(YooKassaOptions options) =>
        !string.IsNullOrWhiteSpace(options.ShopId) &&
        !string.IsNullOrWhiteSpace(options.SecretKey) &&
        !string.IsNullOrWhiteSpace(options.ApiBaseUrl) &&
        IsValidAmount(options.PremiumMonthlyAmount) &&
        IsValidAmount(options.PremiumYearlyAmount) &&
        !string.IsNullOrWhiteSpace(options.Currency) &&
        Uri.IsWellFormedUriString(options.ReturnUrl, UriKind.Absolute);

    private static bool IsValidAmount(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount) &&
        amount > 0;
}
