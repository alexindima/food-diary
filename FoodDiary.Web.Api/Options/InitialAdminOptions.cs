using System.Net.Mail;

namespace FoodDiary.Web.Api.Options;

public sealed class InitialAdminOptions {
    public const string SectionName = "InitialAdmin";

    public string Email { get; init; } = "admin@fooddiary.club";
    public string Password { get; init; } = string.Empty;

    public static bool HasValidConfiguration(InitialAdminOptions options) {
        if (string.IsNullOrWhiteSpace(options.Password)) {
            return true;
        }

        return options.Password.Length >= 12 &&
               !options.Password.Equals("123456", StringComparison.Ordinal) &&
               IsValidEmail(options.Email);
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
