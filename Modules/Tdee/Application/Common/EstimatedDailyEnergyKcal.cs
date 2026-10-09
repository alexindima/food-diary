namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record EstimatedDailyEnergyKcal {
    public double Value { get; }
    private EstimatedDailyEnergyKcal(double value) => Value = value;

    // Read values preserve the existing calculator's legacy-data and fallback policy.
    public static EstimatedDailyEnergyKcal? FromOptional(double? value) => value.HasValue ? new(value.Value) : null;
}
