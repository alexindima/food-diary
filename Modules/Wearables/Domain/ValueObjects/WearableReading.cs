using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Domain.ValueObjects;

public sealed record WearableReading {
    public WearableDataType DataType { get; }
    public double Value { get; }

    private WearableReading(WearableDataType dataType, double value) {
        DataType = dataType;
        Value = DomainGuard.NonNegativeFinite(value, nameof(value));
    }

    public static WearableReading Steps(double value) => new(WearableDataType.Steps, value);
    public static WearableReading HeartRate(double value) => new(WearableDataType.HeartRate, value);
    public static WearableReading CaloriesBurned(double value) => new(WearableDataType.CaloriesBurned, value);
    public static WearableReading ActiveMinutes(double value) => new(WearableDataType.ActiveMinutes, value);
    public static WearableReading SleepMinutes(double value) => new(WearableDataType.SleepMinutes, value);

    public static WearableReading FromFields(WearableDataType dataType, double value) {
        DomainGuard.Defined(dataType, nameof(dataType));
        return new WearableReading(dataType, value);
    }
}
