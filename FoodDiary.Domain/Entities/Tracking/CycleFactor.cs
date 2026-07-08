using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleFactor : Entity<CycleFactorId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public CycleFactorType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? Notes { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private CycleFactor() {
    }

    private CycleFactor(CycleFactorId id) : base(id) {
    }

    public static CycleFactor Create(CycleProfileId cycleProfileId, CycleFactorType type, DateTime startDate, DateTime? endDate, string? notes) {
        EnsureCycleProfileId(cycleProfileId);
        EnsureDefined(type, nameof(type));
        DateTime normalizedStart = CycleProfile.NormalizeDate(startDate);
        DateTime? normalizedEnd = endDate.HasValue ? CycleProfile.NormalizeDate(endDate.Value) : null;
        EnsureRange(normalizedStart, normalizedEnd);

        var factor = new CycleFactor(CycleFactorId.New()) {
            CycleProfileId = cycleProfileId,
            Type = type,
            StartDate = normalizedStart,
            EndDate = normalizedEnd,
            Notes = CycleProfile.NormalizeNotes(notes),
        };

        factor.SetCreated();
        return factor;
    }

    public void Update(DateTime? endDate, string? notes, bool clearNotes) {
        DateTime? normalizedEnd = endDate.HasValue ? CycleProfile.NormalizeDate(endDate.Value) : null;
        EnsureRange(StartDate, normalizedEnd);
        EndDate = normalizedEnd;
        if (clearNotes) {
            Notes = null;
        } else if (notes is not null) {
            Notes = CycleProfile.NormalizeNotes(notes);
        }
        SetModified();
    }

    private static void EnsureRange(DateTime startDate, DateTime? endDate) {
        if (endDate is not null && endDate.Value < startDate) {
            throw new ArgumentOutOfRangeException(nameof(endDate), "End date must be later than or equal to start date.");
        }
    }

    private static void EnsureCycleProfileId(CycleProfileId cycleProfileId) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum {
        if (!Enum.IsDefined(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
        }
    }
}
