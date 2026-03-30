using FoodDiary.Domain.Common;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class Cycle : AggregateRoot<CycleId> {
    private const int DefaultCycleLength = 28;
    private const int DefaultLutealLength = 14;

    private readonly List<CycleDay> _days = [];

    public UserId UserId { get; private set; }
    public DateTime StartDate { get; private set; }
    public int AverageLength { get; private set; }
    public int LutealLength { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<CycleDay> Days => _days.AsReadOnly();

    private Cycle() {
    }

    private Cycle(CycleId id) : base(id) {
    }

    public static Cycle Create(
        UserId userId,
        DateTime startDate,
        int? averageLength = null,
        int? lutealLength = null,
        string? notes = null) {
        EnsureUserId(userId);

        var cycle = new Cycle(CycleId.New()) {
            UserId = userId,
            StartDate = NormalizeDate(startDate),
            AverageLength = NormalizeAverageLength(averageLength),
            LutealLength = NormalizeLutealLength(lutealLength),
            Notes = NormalizeNotes(notes)
        };

        cycle.SetCreated();
        return cycle;
    }

    public void UpdateLengths(int? averageLength = null, int? lutealLength = null, string? notes = null, bool clearNotes = false) {
        var changed = false;
        var normalizedNotes = NormalizeNotes(notes);

        EnsureClearConflict(clearNotes, normalizedNotes, nameof(clearNotes), nameof(notes));

        if (averageLength.HasValue) {
            var normalizedAverageLength = NormalizeAverageLength(averageLength);
            if (AverageLength != normalizedAverageLength) {
                AverageLength = normalizedAverageLength;
                changed = true;
            }
        }

        if (lutealLength.HasValue) {
            var normalizedLutealLength = NormalizeLutealLength(lutealLength);
            if (LutealLength != normalizedLutealLength) {
                LutealLength = normalizedLutealLength;
                changed = true;
            }
        }

        if (clearNotes) {
            if (Notes is not null) {
                Notes = null;
                changed = true;
            }
        } else if (notes is not null) {
            if (Notes != normalizedNotes) {
                Notes = normalizedNotes;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    public CycleDay AddOrUpdateDay(
        DateTime date,
        bool isPeriod,
        DailySymptoms symptoms,
        string? notes = null,
        bool clearNotes = false) {
        var normalizedDate = NormalizeDate(date);
        var normalizedNotes = NormalizeNotes(notes);
        EnsureClearConflict(clearNotes, normalizedNotes, nameof(clearNotes), nameof(notes));
        var existing = _days.FirstOrDefault(d => d.Date == normalizedDate);
        if (existing is not null) {
            var hasChanges =
                existing.IsPeriod != isPeriod ||
                !existing.Symptoms.Equals(symptoms) ||
                existing.Notes != (clearNotes ? null : normalizedNotes);

            if (!hasChanges) {
                return existing;
            }

            existing.Update(isPeriod, symptoms, normalizedNotes, clearNotes);
            RaiseDomainEvent(new CycleDayUpsertedDomainEvent(Id, normalizedDate, IsCreated: false));
            SetModified();
            return existing;
        }

        var day = CycleDay.Create(Id, normalizedDate, isPeriod, symptoms, clearNotes ? null : normalizedNotes);
        _days.Add(day);
        RaiseDomainEvent(new CycleDayUpsertedDomainEvent(Id, normalizedDate, IsCreated: true));
        SetModified();
        return day;
    }

    public bool RemoveDay(DateTime date) {
        var normalizedDate = NormalizeDate(date);
        var existing = _days.FirstOrDefault(d => d.Date == normalizedDate);
        if (existing is null) {
            return false;
        }

        _days.Remove(existing);
        RaiseDomainEvent(new CycleDayRemovedDomainEvent(Id, normalizedDate));
        SetModified();
        return true;
    }

    private static DateTime NormalizeDate(DateTime value) {
        if (value.Kind == DateTimeKind.Unspecified) {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        var utc = value.ToUniversalTime();

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private static int NormalizeAverageLength(int? value) {
        var length = value ?? DefaultCycleLength;
        return length is < 18 or > 60
            ? throw new ArgumentOutOfRangeException(nameof(value), "Average cycle length must be in range [18, 60].")
            : length;
    }

    private static int NormalizeLutealLength(int? value) {
        var length = value ?? DefaultLutealLength;
        return length is < 8 or > 18
            ? throw new ArgumentOutOfRangeException(nameof(value), "Luteal length must be in range [8, 18].")
            : length;
    }

    private static string? NormalizeNotes(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : class {
        if (clear && value is not null) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }

}
