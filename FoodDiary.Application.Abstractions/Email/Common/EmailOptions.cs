namespace FoodDiary.Application.Email.Common;

public sealed class EmailOptions {
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "FoodDiary";
    public string FrontendBaseUrl { get; init; } = string.Empty;
    public string VerificationPath { get; init; } = "/verify-email";
    public string PasswordResetPath { get; init; } = "/reset-password";

    public static bool HasValidFrontendBaseUrl(EmailOptions options) {
        return string.IsNullOrWhiteSpace(options.FrontendBaseUrl) ||
               Uri.IsWellFormedUriString(options.FrontendBaseUrl, UriKind.Absolute);
    }

    public static bool HasVerificationPath(EmailOptions options) => !string.IsNullOrWhiteSpace(options.VerificationPath);

    public static bool HasPasswordResetPath(EmailOptions options) => !string.IsNullOrWhiteSpace(options.PasswordResetPath);
}
