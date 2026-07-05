namespace FoodDiary.Integrations.Options;

public sealed class GoogleFitOptions {
    public const string SectionName = "GoogleFit";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;

    public static bool IsEmptyOrComplete(GoogleFitOptions options) {
        bool anyConfigured = !string.IsNullOrWhiteSpace(options.ClientId) ||
                             !string.IsNullOrWhiteSpace(options.ClientSecret) ||
                             !string.IsNullOrWhiteSpace(options.RedirectUri);
        return !anyConfigured ||
               (!string.IsNullOrWhiteSpace(options.ClientId) &&
                !string.IsNullOrWhiteSpace(options.ClientSecret) &&
                Uri.IsWellFormedUriString(options.RedirectUri, UriKind.Absolute));
    }
}
