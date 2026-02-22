using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Content;

public sealed class DailyAdvice : AggregateRoot<DailyAdviceId> {
    private const int ValueMaxLength = 512;
    private const int LocaleMaxLength = 10;
    private const int TagMaxLength = 64;

    public string Value { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public int Weight { get; private set; }
    public string? Tag { get; private set; }

    private DailyAdvice() {
    }

    private DailyAdvice(DailyAdviceId id) : base(id) {
    }

    public static DailyAdvice Create(string value, string locale, int weight = 1, string? tag = null) {
        var normalizedValue = NormalizeRequired(
            value,
            nameof(value),
            "Advice value cannot be empty.",
            ValueMaxLength);
        var normalizedLocale = NormalizeLocale(locale);
        var normalizedTag = NormalizeOptional(tag, nameof(tag), TagMaxLength);
        var normalizedWeight = Math.Max(1, weight);

        var advice = new DailyAdvice(DailyAdviceId.New()) {
            Value = normalizedValue,
            Locale = normalizedLocale,
            Weight = normalizedWeight,
            Tag = normalizedTag
        };

        advice.SetCreated();
        return advice;
    }

    public void Update(string? value = null, string? locale = null, int? weight = null, string? tag = null) {
        var changed = false;

        if (value is not null) {
            var normalizedValue = NormalizeRequired(
                value,
                nameof(value),
                "Advice value cannot be empty.",
                ValueMaxLength);

            if (Value != normalizedValue) {
                Value = normalizedValue;
                changed = true;
            }
        }

        if (locale is not null) {
            var normalizedLocale = NormalizeLocale(locale);
            if (Locale != normalizedLocale) {
                Locale = normalizedLocale;
                changed = true;
            }
        }

        if (weight.HasValue) {
            var normalizedWeight = Math.Max(1, weight.Value);
            if (Weight != normalizedWeight) {
                Weight = normalizedWeight;
                changed = true;
            }
        }

        if (tag is not null) {
            var normalizedTag = NormalizeOptional(tag, nameof(tag), TagMaxLength);
            if (Tag != normalizedTag) {
                Tag = normalizedTag;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static string NormalizeRequired(string value, string paramName, string message, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException(message, paramName);
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static string NormalizeLocale(string value) {
        var normalized = NormalizeRequired(
            value,
            nameof(value),
            "Locale is required.",
            LocaleMaxLength).ToLowerInvariant();

        var separatorIndex = normalized.IndexOfAny(['-', '_']);
        var primaryLanguage = separatorIndex > 0 ? normalized[..separatorIndex] : normalized;

        return !LanguageCode.TryParse(primaryLanguage, out var languageCode)
            ? throw new ArgumentOutOfRangeException(nameof(value), "Locale must be one of the supported language codes.")
            : languageCode.Value;
    }

    private static string? NormalizeOptional(string? value, string paramName, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }
}
