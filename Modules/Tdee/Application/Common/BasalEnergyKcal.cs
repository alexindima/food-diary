namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record BasalEnergyKcal {
    public double Value { get; }
    private BasalEnergyKcal(double value) => Value = value;

    // Read values preserve the existing calculator's legacy-data and fallback policy.
    public static BasalEnergyKcal? FromOptional(double? value) => value.HasValue ? new(value.Value) : null;
}
