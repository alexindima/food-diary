using System.Linq;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Tracking;

/// <summary>
/// Menstrual cycle aggregate.
/// </summary>
public sealed class Cycle : AggregateRoot<CycleId>
{
    private const int DefaultCycleLength = 28;
    private const int DefaultLutealLength = 14;

    private readonly List<CycleDay> _days = new();

    public UserId UserId { get; private set; }
    public DateTime StartDate { get; private set; }
    public int AverageLength { get; private set; }
    public int LutealLength { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<CycleDay> Days => _days.AsReadOnly();

    private Cycle()
    {
    }

    private Cycle(CycleId id) : base(id)
    {
    }

    public static Cycle Create(
        UserId userId,
        DateTime startDate,
        int? averageLength = null,
        int? lutealLength = null,
        string? notes = null)
    {
        var cycle = new Cycle(CycleId.New())
        {
            UserId = userId,
            StartDate = NormalizeDate(startDate),
            AverageLength = NormalizeAverageLength(averageLength),
            LutealLength = NormalizeLutealLength(lutealLength),
            Notes = NormalizeNotes(notes)
        };

        cycle.SetCreated();
        return cycle;
    }

    public void UpdateLengths(int? averageLength = null, int? lutealLength = null, string? notes = null)
    {
        if (averageLength.HasValue)
        {
            AverageLength = NormalizeAverageLength(averageLength);
        }

        if (lutealLength.HasValue)
        {
            LutealLength = NormalizeLutealLength(lutealLength);
        }

        if (notes is not null)
        {
            Notes = NormalizeNotes(notes);
        }

        SetModified();
    }

    public CycleDay AddOrUpdateDay(
        DateTime date,
        bool isPeriod,
        DailySymptoms symptoms,
        string? notes = null)
    {
        var normalizedDate = NormalizeDate(date);
        var existing = _days.FirstOrDefault(d => d.Date == normalizedDate);
        if (existing is not null)
        {
            existing.Update(isPeriod, symptoms, notes);
            RaiseDomainEvent(new CycleDayUpsertedDomainEvent(Id, normalizedDate, IsCreated: false));
            SetModified();
            return existing;
        }

        var day = CycleDay.Create(Id, normalizedDate, isPeriod, symptoms, notes);
        _days.Add(day);
        RaiseDomainEvent(new CycleDayUpsertedDomainEvent(Id, normalizedDate, IsCreated: true));
        SetModified();
        return day;
    }

    public bool RemoveDay(DateTime date)
    {
        var normalizedDate = NormalizeDate(date);
        var existing = _days.FirstOrDefault(d => d.Date == normalizedDate);
        if (existing is null)
        {
            return false;
        }

        _days.Remove(existing);
        RaiseDomainEvent(new CycleDayRemovedDomainEvent(Id, normalizedDate));
        SetModified();
        return true;
    }

    private static DateTime NormalizeDate(DateTime value)
    {
        var dateOnly = value.Date;
        return dateOnly.Kind == DateTimeKind.Utc
            ? dateOnly
            : DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);
    }

    private static int NormalizeAverageLength(int? value)
    {
        var length = value ?? DefaultCycleLength;
        return Math.Clamp(length, 18, 60);
    }

    private static int NormalizeLutealLength(int? value)
    {
        var length = value ?? DefaultLutealLength;
        return Math.Clamp(length, 8, 18);
    }

    private static string? NormalizeNotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

