namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record DailyCalorieTargetKcal {
    public double Value { get; }
    private DailyCalorieTargetKcal(double value) => Value = value;

    // Read values preserve the existing calculator's legacy-data and fallback policy.
    public static DailyCalorieTargetKcal? FromOptional(double? value) => value.HasValue ? new(value.Value) : null;
}
