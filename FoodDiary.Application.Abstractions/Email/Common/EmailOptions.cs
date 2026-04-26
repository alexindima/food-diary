namespace FoodDiary.Application.Abstractions.Email.Common;

public sealed class EmailOptions {
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "FoodDiary";
    public string FrontendBaseUrl { get; init; } = string.Empty;
    public string[] AllowedFrontendBaseUrls { get; init; } = [];
    public string VerificationPath { get; init; } = "/verify-email";
    public string PasswordResetPath { get; init; } = "/reset-password";

    public static bool HasValidFrontendBaseUrl(EmailOptions options) {
        return string.IsNullOrWhiteSpace(options.FrontendBaseUrl) ||
               IsHttpUrl(options.FrontendBaseUrl);
    }

    public static bool HasValidAllowedFrontendBaseUrls(EmailOptions options) {
        return options.AllowedFrontendBaseUrls.All(static value =>
            !string.IsNullOrWhiteSpace(value) &&
            IsHttpUrl(value));
    }

    public static bool HasVerificationPath(EmailOptions options) => !string.IsNullOrWhiteSpace(options.VerificationPath);

    public static bool HasPasswordResetPath(EmailOptions options) => !string.IsNullOrWhiteSpace(options.PasswordResetPath);

    private static bool IsHttpUrl(string value) {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
