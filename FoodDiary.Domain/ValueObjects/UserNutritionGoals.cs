namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserNutritionGoals(
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal) {
    public static UserNutritionGoals Create(
        double? dailyCalorieTarget,
        double? proteinTarget,
        double? fatTarget,
        double? carbTarget,
        double? fiberTarget,
        double? waterGoal) {
        return new UserNutritionGoals(
            EnsureNonNegative(dailyCalorieTarget, nameof(dailyCalorieTarget)),
            EnsureNonNegative(proteinTarget, nameof(proteinTarget)),
            EnsureNonNegative(fatTarget, nameof(fatTarget)),
            EnsureNonNegative(carbTarget, nameof(carbTarget)),
            EnsureNonNegative(fiberTarget, nameof(fiberTarget)),
            EnsureNonNegative(waterGoal, nameof(waterGoal)));
    }

    public UserNutritionGoals With(
        double? dailyCalorieTarget = null,
        double? proteinTarget = null,
        double? fatTarget = null,
        double? carbTarget = null,
        double? fiberTarget = null,
        double? waterGoal = null) {
        return new UserNutritionGoals(
            dailyCalorieTarget.HasValue
                ? EnsureNonNegative(dailyCalorieTarget, nameof(dailyCalorieTarget))
                : DailyCalorieTarget,
            proteinTarget.HasValue
                ? EnsureNonNegative(proteinTarget, nameof(proteinTarget))
                : ProteinTarget,
            fatTarget.HasValue
                ? EnsureNonNegative(fatTarget, nameof(fatTarget))
                : FatTarget,
            carbTarget.HasValue
                ? EnsureNonNegative(carbTarget, nameof(carbTarget))
                : CarbTarget,
            fiberTarget.HasValue
                ? EnsureNonNegative(fiberTarget, nameof(fiberTarget))
                : FiberTarget,
            waterGoal.HasValue
                ? EnsureNonNegative(waterGoal, nameof(waterGoal))
                : WaterGoal);
    }

    private static double? EnsureNonNegative(double? value, string paramName) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value))) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value is < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }
}
