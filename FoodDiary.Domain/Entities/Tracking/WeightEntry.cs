using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Tracking;

/// <summary>
/// Ð•Ð¶ÐµÐ´Ð½ÐµÐ²Ð½Ð°Ñ Ð·Ð°Ð¿Ð¸ÑÑŒ Ð²ÐµÑÐ° Ð¿Ð¾Ð»ÑŒÐ·Ð¾Ð²Ð°Ñ‚ÐµÐ»Ñ
/// </summary>
public sealed class WeightEntry : AggregateRoot<WeightEntryId>
{
    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double Weight { get; private set; }

    public User User { get; private set; } = null!;

    private WeightEntry()
    {
    }

    private WeightEntry(WeightEntryId id) : base(id)
    {
    }

    public static WeightEntry Create(UserId userId, DateTime date, double weight)
    {
        var entry = new WeightEntry(WeightEntryId.New())
        {
            UserId = userId,
            Date = NormalizeDate(date),
            Weight = weight
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? weight = null, DateTime? date = null)
    {
        if (weight.HasValue)
        {
            Weight = weight.Value;
        }

        if (date.HasValue)
        {
            Date = NormalizeDate(date.Value);
        }

        SetModified();
    }

    private static DateTime NormalizeDate(DateTime value)
    {
        var dateOnly = value.Date;
        return dateOnly.Kind == DateTimeKind.Utc
            ? dateOnly
            : DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
    }
}

