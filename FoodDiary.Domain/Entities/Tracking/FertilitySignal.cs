using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class FertilitySignal : Entity<FertilitySignalId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateTime Date { get; private set; }
    public double? BasalBodyTemperatureCelsius { get; private set; }
    public OvulationTestResult? OvulationTestResult { get; private set; }
    public string? CervicalFluid { get; private set; }
    public bool? HadSex { get; private set; }
    public string? Notes { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;

    private FertilitySignal() {
    }

    private FertilitySignal(FertilitySignalId id) : base(id) {
    }

    public static FertilitySignal Create(
        CycleProfileId cycleProfileId,
        DateTime date,
        double? basalBodyTemperatureCelsius,
        OvulationTestResult? ovulationTestResult,
        string? cervicalFluid,
        bool? hadSex,
        string? notes) {
        EnsureCycleProfileId(cycleProfileId);
        EnsureTemperature(basalBodyTemperatureCelsius);
        EnsureOptionalDefined(ovulationTestResult, nameof(ovulationTestResult));

        var signal = new FertilitySignal(FertilitySignalId.New()) {
            CycleProfileId = cycleProfileId,
            Date = CycleProfile.NormalizeDate(date),
            BasalBodyTemperatureCelsius = basalBodyTemperatureCelsius,
            OvulationTestResult = ovulationTestResult,
            CervicalFluid = CycleProfile.NormalizeNotes(cervicalFluid),
            HadSex = hadSex,
            Notes = CycleProfile.NormalizeNotes(notes),
        };

        signal.SetCreated();
        return signal;
    }

    public void Update(
        double? basalBodyTemperatureCelsius,
        OvulationTestResult? ovulationTestResult,
        string? cervicalFluid,
        bool? hadSex,
        string? notes,
        bool clearNotes) {
        EnsureTemperature(basalBodyTemperatureCelsius);
        EnsureOptionalDefined(ovulationTestResult, nameof(ovulationTestResult));
        BasalBodyTemperatureCelsius = basalBodyTemperatureCelsius;
        OvulationTestResult = ovulationTestResult;
        CervicalFluid = CycleProfile.NormalizeNotes(cervicalFluid);
        HadSex = hadSex;
        if (clearNotes) {
            Notes = null;
        } else if (notes is not null) {
            Notes = CycleProfile.NormalizeNotes(notes);
        }
        SetModified();
    }

    private static void EnsureTemperature(double? value) {
        if (!value.HasValue) {
            return;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value is < 34 or > 42) {
            throw new ArgumentOutOfRangeException(nameof(value), "Basal body temperature must be in range [34, 42].");
        }
    }

    private static void EnsureCycleProfileId(CycleProfileId cycleProfileId) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }
    }

    private static void EnsureOptionalDefined<TEnum>(TEnum? value, string paramName)
        where TEnum : struct, Enum {
        if (value.HasValue && !Enum.IsDefined(value.Value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
        }
    }
}
