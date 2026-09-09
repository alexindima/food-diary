using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.Entities.Billing;

internal static class BillingDomainGuard {
    public static decimal? OptionalNumeric18Scale2(decimal? value, string paramName) {
        const decimal maxNumeric18Scale2 = 9_999_999_999_999_999.99m;
        if (!value.HasValue) {
            return null;
        }

        decimal normalized = value.Value;
        if (normalized is < -maxNumeric18Scale2 or > maxNumeric18Scale2) {
            throw new ArgumentOutOfRangeException(paramName, "Value exceeds numeric(18,2) storage limits.");
        }

        return decimal.Round(normalized, 2, MidpointRounding.ToEven) != normalized
            ? throw new ArgumentOutOfRangeException(paramName, "Value must have at most two fractional digits.")
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
