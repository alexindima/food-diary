using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

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
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Advice value cannot be empty.", nameof(value));
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new ArgumentException("Locale is required.", nameof(locale));
        }

        var advice = new DailyAdvice(DailyAdviceId.New())
        {
            Value = value.Trim(),
            Locale = locale.Trim().ToLowerInvariant(),
            Weight = Math.Max(1, weight),
            Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim()
        };

        advice.SetCreated();
        return advice;
    }
}
