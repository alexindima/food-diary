using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Content;

/// <summary>
/// Localized daily advice item shown on the dashboard.
/// </summary>
public sealed class DailyAdvice : AggregateRoot<DailyAdviceId>
{
    public string Value { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public int Weight { get; private set; }
    public string? Tag { get; private set; }

    private DailyAdvice()
    {
    }

    private DailyAdvice(DailyAdviceId id) : base(id)
    {
    }

    public static DailyAdvice Create(string value, string locale, int weight = 1, string? tag = null)
    {
        var normalizedValue = NormalizeRequired(value, nameof(value), "Advice value cannot be empty.");
        var normalizedLocale = NormalizeRequired(locale, nameof(locale), "Locale is required.").ToLowerInvariant();
        var normalizedTag = NormalizeOptional(tag);
        var normalizedWeight = Math.Max(1, weight);

        var advice = new DailyAdvice(DailyAdviceId.New())
        {
            Value = normalizedValue,
            Locale = normalizedLocale,
            Weight = normalizedWeight,
            Tag = normalizedTag
        };

        advice.SetCreated();
        return advice;
    }

    public void Update(string? value = null, string? locale = null, int? weight = null, string? tag = null)
    {
        if (value is not null)
        {
            Value = NormalizeRequired(value, nameof(value), "Advice value cannot be empty.");
        }

        if (locale is not null)
        {
            Locale = NormalizeRequired(locale, nameof(locale), "Locale is required.").ToLowerInvariant();
        }

        if (weight.HasValue)
        {
            Weight = Math.Max(1, weight.Value);
        }

        if (tag is not null)
        {
            Tag = NormalizeOptional(tag);
        }

        SetModified();
    }

    private static string NormalizeRequired(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

