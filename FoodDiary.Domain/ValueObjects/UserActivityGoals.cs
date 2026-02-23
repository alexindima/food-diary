namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserActivityGoals(
    int? StepGoal,
    double? HydrationGoal) {
    public static UserActivityGoals Create(int? stepGoal, double? hydrationGoal) {
        return new UserActivityGoals(
            EnsureNonNegative(stepGoal, nameof(stepGoal)),
            EnsureNonNegative(hydrationGoal, nameof(hydrationGoal)));
    }

    public UserActivityGoals With(
        int? stepGoal = null,
        double? hydrationGoal = null) {
        return new UserActivityGoals(
            stepGoal.HasValue
                ? EnsureNonNegative(stepGoal, nameof(stepGoal))
                : StepGoal,
            hydrationGoal.HasValue
                ? EnsureNonNegative(hydrationGoal, nameof(hydrationGoal))
                : HydrationGoal);
    }

    private static int? EnsureNonNegative(int? value, string paramName) {
        return value is < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
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
