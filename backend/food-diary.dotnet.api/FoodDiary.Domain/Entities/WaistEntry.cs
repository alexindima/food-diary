using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Measurement of waist circumference for WHtR tracking.
/// </summary>
public sealed class WaistEntry : AggregateRoot<WaistEntryId>
{
    public UserId UserId { get; private set; }
    public DateTime Date { get; private set; }
    public double Circumference { get; private set; }

    public User User { get; private set; } = null!;

    private WaistEntry()
    {
    }

    private WaistEntry(WaistEntryId id) : base(id)
    {
    }

    public static WaistEntry Create(UserId userId, DateTime date, double circumference)
    {
        var entry = new WaistEntry(WaistEntryId.New())
        {
            UserId = userId,
            Date = NormalizeDate(date),
            Circumference = circumference,
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(double? circumference = null, DateTime? date = null)
    {
        if (circumference.HasValue)
        {
            Circumference = circumference.Value;
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
