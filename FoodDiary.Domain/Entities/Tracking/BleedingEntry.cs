using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class BleedingEntry : Entity<BleedingEntryId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateTime Date { get; private set; }
    public BleedingType Type { get; private set; }
    public CycleFlowLevel Flow { get; private set; }
    public int? PainImpact { get; private set; }
    public string? Notes { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private BleedingEntry() {
    }

    private BleedingEntry(BleedingEntryId id) : base(id) {
    }

    public static BleedingEntry Create(CycleProfileId cycleProfileId, DateTime date, BleedingType type, CycleFlowLevel flow, int? painImpact, string? notes) {
        EnsureCycleProfileId(cycleProfileId);
        EnsureDefined(type, nameof(type));
        EnsureDefined(flow, nameof(flow));

        var entry = new BleedingEntry(BleedingEntryId.New()) {
            CycleProfileId = cycleProfileId,
            Date = CycleProfile.NormalizeDate(date),
            Type = type,
            Flow = flow,
            PainImpact = painImpact.HasValue ? CycleProfile.NormalizeIntensity(painImpact.Value, nameof(painImpact)) : null,
            Notes = CycleProfile.NormalizeNotes(notes),
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(CycleFlowLevel flow, int? painImpact, string? notes, bool clearNotes) {
        EnsureDefined(flow, nameof(flow));
        Flow = flow;
        PainImpact = painImpact.HasValue ? CycleProfile.NormalizeIntensity(painImpact.Value, nameof(painImpact)) : null;
        if (clearNotes) {
            Notes = null;
        } else if (notes is not null) {
            Notes = CycleProfile.NormalizeNotes(notes);
        }
        SetModified();
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
