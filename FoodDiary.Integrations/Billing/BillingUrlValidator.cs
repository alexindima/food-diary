namespace FoodDiary.Integrations.Billing;

internal static class BillingUrlValidator {
    public static bool IsAbsoluteHttps(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrEmpty(uri.UserInfo);
}
