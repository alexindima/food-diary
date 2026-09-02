using System.Runtime.InteropServices;

namespace FoodDiary.Domain.ValueObjects;

[StructLayout(LayoutKind.Auto)]
public readonly record struct UserNutritionGoals {
    public double? DailyCalorieTarget { get; }
    public double? ProteinTarget { get; }
    public double? FatTarget { get; }
    public double? CarbTarget { get; }
    public double? FiberTarget { get; }
    public double? WaterGoal { get; }

    public UserNutritionGoals(
        double? dailyCalorieTarget,
        double? proteinTarget,
        double? fatTarget,
        double? carbTarget,
        double? fiberTarget,
        double? waterGoal) {
        DailyCalorieTarget = EnsureNonNegative(dailyCalorieTarget, nameof(dailyCalorieTarget));
        ProteinTarget = EnsureNonNegative(proteinTarget, nameof(proteinTarget));
        FatTarget = EnsureNonNegative(fatTarget, nameof(fatTarget));
        CarbTarget = EnsureNonNegative(carbTarget, nameof(carbTarget));
        FiberTarget = EnsureNonNegative(fiberTarget, nameof(fiberTarget));
        WaterGoal = EnsureNonNegative(waterGoal, nameof(waterGoal));
    }

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
