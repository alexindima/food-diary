using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleDay : Entity<CycleDayId> {
    public CycleId CycleId { get; private set; }
    public DateTime Date { get; private set; }
    public bool IsPeriod { get; private set; }
    public DailySymptoms Symptoms { get; private set; } = null!;
    public string? Notes { get; private set; }

    public Cycle Cycle { get; private set; } = null!;

    private CycleDay() {
    }

    private CycleDay(CycleDayId id) : base(id) {
    }

    public static CycleDay Create(
        CycleId cycleId,
        DateTime date,
        bool isPeriod,
        DailySymptoms symptoms,
        string? notes) {
        EnsureCycleId(cycleId);
        EnsureSymptoms(symptoms);

        var day = new CycleDay(CycleDayId.New()) {
            CycleId = cycleId,
            Date = NormalizeDate(date),
            IsPeriod = isPeriod,
            Symptoms = symptoms,
            Notes = NormalizeNotes(notes)
        };

        day.SetCreated();
        return day;
    }

    public void Update(bool? isPeriod = null, DailySymptoms? symptoms = null, string? notes = null, bool clearNotes = false) {
        var changed = false;
        var normalizedNotes = NormalizeNotes(notes);

        EnsureClearConflict(clearNotes, normalizedNotes, nameof(clearNotes), nameof(notes));

        if (isPeriod.HasValue) {
            if (IsPeriod != isPeriod.Value) {
                IsPeriod = isPeriod.Value;
                changed = true;
            }
        }

        if (symptoms is not null) {
            EnsureSymptoms(symptoms);
            if (!Symptoms.Equals(symptoms)) {
                Symptoms = symptoms;
                changed = true;
            }
        }

        if (clearNotes) {
            if (Notes is not null) {
                Notes = null;
                changed = true;
            }
        }
        else if (notes is not null) {
            if (Notes != normalizedNotes) {
                Notes = normalizedNotes;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    private static DateTime NormalizeDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private static string? NormalizeNotes(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void EnsureCycleId(CycleId cycleId) {
        if (cycleId == CycleId.Empty) {
            throw new ArgumentException("CycleId is required.", nameof(cycleId));
        }
    }

    private static void EnsureSymptoms(DailySymptoms symptoms) {
        ArgumentNullException.ThrowIfNull(symptoms);
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : class {
        if (clear && value is not null) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }

}
