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

    public static bool HasAnyConfiguration(YooKassaOptions options) =>
        !string.IsNullOrWhiteSpace(options.ShopId) ||
        !string.IsNullOrWhiteSpace(options.SecretKey) ||
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyAmount) ||
        !string.IsNullOrWhiteSpace(options.PremiumYearlyAmount);

    public static bool HasValidCheckoutConfiguration(YooKassaOptions options) =>
        !string.IsNullOrWhiteSpace(options.ShopId) &&
        !string.IsNullOrWhiteSpace(options.SecretKey) &&
        HasValidApiBaseUrl(options.ApiBaseUrl) &&
        IsValidAmount(options.PremiumMonthlyAmount) &&
        IsValidAmount(options.PremiumYearlyAmount) &&
        !string.IsNullOrWhiteSpace(options.Currency) &&
        Billing.BillingUrlValidator.IsAbsoluteHttps(options.ReturnUrl);

    private static bool HasValidApiBaseUrl(string? value) =>
        IntegrationUriValidator.IsAbsoluteHttpsBaseUrl(value);

    private static bool IsValidAmount(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) &&
        amount > 0;
}
