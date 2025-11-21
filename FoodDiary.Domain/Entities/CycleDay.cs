using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Specific day of a menstrual cycle with symptom tracking.
/// </summary>
public sealed class CycleDay : Entity<CycleDayId>
{
    public CycleId CycleId { get; private set; }
    public DateTime Date { get; private set; }
    public bool IsPeriod { get; private set; }
    public DailySymptoms Symptoms { get; private set; } = null!;
    public string? Notes { get; private set; }

    public Cycle Cycle { get; private set; } = null!;

    private CycleDay()
    {
    }

    private CycleDay(CycleDayId id) : base(id)
    {
    }

    public static CycleDay Create(
        CycleId cycleId,
        DateTime date,
        bool isPeriod,
        DailySymptoms symptoms,
        string? notes)
    {
        var day = new CycleDay(CycleDayId.New())
        {
            CycleId = cycleId,
            Date = NormalizeDate(date),
            IsPeriod = isPeriod,
            Symptoms = symptoms,
            Notes = notes
        };

        day.SetCreated();
        return day;
    }

    public void Update(bool? isPeriod = null, DailySymptoms? symptoms = null, string? notes = null)
    {
        if (isPeriod.HasValue)
        {
            IsPeriod = isPeriod.Value;
        }

        if (symptoms is not null)
        {
            Symptoms = symptoms;
        }

        if (notes is not null)
        {
            Notes = notes;
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
