using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleFactor : Entity<CycleFactorId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public CycleFactorType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Notes { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private CycleFactor() {
    }

    private CycleFactor(CycleFactorId id) : base(id) {
    }

    public static CycleFactor Create(CycleProfileId cycleProfileId, CycleFactorType type, DateOnly startDate, DateOnly? endDate, string? notes) {
        EnsureCycleProfileId(cycleProfileId);
        EnsureDefined(type, nameof(type));
        EnsureRange(startDate, endDate);

        var factor = new CycleFactor(CycleFactorId.New()) {
            CycleProfileId = cycleProfileId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Notes = CycleProfile.NormalizeNotes(notes),
        };

        factor.SetCreated();
        return factor;
    }

    public void Update(DateOnly? endDate, string? notes, bool clearNotes) {
        EnsureRange(StartDate, endDate);
        string? normalizedNotes = notes is not null ? CycleProfile.NormalizeNotes(notes) : Notes;

        EndDate = endDate;
        if (clearNotes) {
            Notes = null;
        } else if (notes is not null) {
            Notes = normalizedNotes;
        }
        SetModified();
    }

    private static void EnsureRange(DateOnly startDate, DateOnly? endDate) {
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
