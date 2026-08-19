namespace FoodDiary.Integrations.Options;

public sealed class FitbitOptions {
    public const string SectionName = "Fitbit";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;

    public static bool IsEmptyOrComplete(FitbitOptions options) {
        return !HasAnyConfiguration(options) || HasCompleteConfiguration(options);
    }

    public static bool HasCompleteConfiguration(FitbitOptions options) =>
        !string.IsNullOrWhiteSpace(options.ClientId) &&
        !string.IsNullOrWhiteSpace(options.ClientSecret) &&
        IntegrationUriValidator.IsSecureRedirectUrl(options.RedirectUri);

    private static bool HasAnyConfiguration(FitbitOptions options) =>
        !string.IsNullOrWhiteSpace(options.ClientId) ||
        !string.IsNullOrWhiteSpace(options.ClientSecret) ||
        !string.IsNullOrWhiteSpace(options.RedirectUri);
}
