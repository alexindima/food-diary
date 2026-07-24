using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Initializer;

internal sealed record InitialAdminBootstrapOptions(
    string Email,
    string Password,
    TimeSpan Timeout) {
    private const int DefaultTimeoutSeconds = 30;
    private const int MaximumTimeoutSeconds = 300;

    public static InitialAdminBootstrapOptions FromConfiguration(IConfiguration configuration) {
        string email = configuration["InitialAdmin:Email"] ?? "admin@fooddiary.club";
        string password = configuration["InitialAdmin:Password"] ?? string.Empty;
        int timeoutSeconds = configuration.GetValue("InitialAdmin:BootstrapTimeoutSeconds", DefaultTimeoutSeconds);

        if (timeoutSeconds is < 1 or > MaximumTimeoutSeconds) {
            throw new InvalidOperationException(
                $"InitialAdmin:BootstrapTimeoutSeconds must be between 1 and {MaximumTimeoutSeconds}.");
        }

        if (!string.IsNullOrWhiteSpace(password) &&
            (password.Length < 12 ||
             password.Equals("123456", StringComparison.Ordinal) ||
             !IsValidEmail(email))) {
            throw new InvalidOperationException(
                "InitialAdmin requires a valid email and a password of at least 12 characters when configured.");
        }

        return new InitialAdminBootstrapOptions(email, password, TimeSpan.FromSeconds(timeoutSeconds));
    }

    private static bool IsValidEmail(string value) {
        try {
            return !string.IsNullOrWhiteSpace(value) &&
                   new MailAddress(value).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);
        } catch {
            return false;
        }
    }
}
