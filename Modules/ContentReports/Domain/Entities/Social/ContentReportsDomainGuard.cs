namespace FoodDiary.Domain.Entities.Social;

internal static class ContentReportsDomainGuard {
    public static void Defined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum {
        if (!Enum.IsDefined(value)) {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }

    public static string? OptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, FormattableString.Invariant($"Value must be at most {maxLength} characters."))
            : normalized;
    }
}
