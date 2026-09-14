using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Billing.Domain.Entities;

internal static class BillingDomainGuard {
    public static decimal? OptionalNumeric19Scale3(decimal? value, string paramName) {
        const decimal maxNumeric19Scale3 = 9_999_999_999_999_999.999m;
        if (!value.HasValue) {
            return null;
        }

        decimal normalized = value.Value;
        if (normalized is < -maxNumeric19Scale3 or > maxNumeric19Scale3) {
            throw new ArgumentOutOfRangeException(paramName, "Value exceeds numeric(19,3) storage limits.");
        }

        return decimal.Round(normalized, 3, MidpointRounding.ToEven) != normalized
            ? throw new ArgumentOutOfRangeException(paramName, "Value must have at most three fractional digits.")
            : normalized;
    }

    public static string? OptionalCurrencyCode(string? value, string paramName) {
        string? normalized = DomainGuard.OptionalText(value, 3, paramName);
        if (normalized is null) {
            return null;
        }

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetter)) {
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", paramName);
        }

        return normalized.ToUpperInvariant();
    }

}
