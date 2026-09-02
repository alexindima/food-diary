namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserCalorieSchedule(
    double? DailyCalorieTarget,
    bool CalorieCyclingEnabled,
    double? MondayCalories,
    double? TuesdayCalories,
    double? WednesdayCalories,
    double? ThursdayCalories,
    double? FridayCalories,
    double? SaturdayCalories,
    double? SundayCalories) {
    private readonly double _weeklyTarget = CalculateWeeklyTarget(
        dailyCalorieTarget: DailyCalorieTarget,
        calorieCyclingEnabled: CalorieCyclingEnabled,
        mondayCalories: MondayCalories,
        tuesdayCalories: TuesdayCalories,
        wednesdayCalories: WednesdayCalories,
        thursdayCalories: ThursdayCalories,
        fridayCalories: FridayCalories,
        saturdayCalories: SaturdayCalories,
        sundayCalories: SundayCalories);

    public double? DailyCalorieTarget { get; } = EnsureNonNegativeFinite(DailyCalorieTarget, nameof(DailyCalorieTarget));
    public bool CalorieCyclingEnabled { get; } = CalorieCyclingEnabled;
    public double? MondayCalories { get; } = EnsureNonNegativeFinite(MondayCalories, nameof(MondayCalories));
    public double? TuesdayCalories { get; } = EnsureNonNegativeFinite(TuesdayCalories, nameof(TuesdayCalories));
    public double? WednesdayCalories { get; } = EnsureNonNegativeFinite(WednesdayCalories, nameof(WednesdayCalories));
    public double? ThursdayCalories { get; } = EnsureNonNegativeFinite(ThursdayCalories, nameof(ThursdayCalories));
    public double? FridayCalories { get; } = EnsureNonNegativeFinite(FridayCalories, nameof(FridayCalories));
    public double? SaturdayCalories { get; } = EnsureNonNegativeFinite(SaturdayCalories, nameof(SaturdayCalories));
    public double? SundayCalories { get; } = EnsureNonNegativeFinite(SundayCalories, nameof(SundayCalories));

    public double? GetTargetForDate(DateTime date) {
        if (!CalorieCyclingEnabled) {
            return DailyCalorieTarget;
        }

        double?[] dayTargets = [
            SundayCalories ?? DailyCalorieTarget,
            MondayCalories ?? DailyCalorieTarget,
            TuesdayCalories ?? DailyCalorieTarget,
            WednesdayCalories ?? DailyCalorieTarget,
            ThursdayCalories ?? DailyCalorieTarget,
            FridayCalories ?? DailyCalorieTarget,
            SaturdayCalories ?? DailyCalorieTarget,
        ];

        return dayTargets[(int)date.DayOfWeek];
    }

    public double GetWeeklyTarget() => _weeklyTarget;

    private static double CalculateWeeklyTarget(
        double? dailyCalorieTarget,
        bool calorieCyclingEnabled,
        double? mondayCalories,
        double? tuesdayCalories,
        double? wednesdayCalories,
        double? thursdayCalories,
        double? fridayCalories,
        double? saturdayCalories,
        double? sundayCalories) {
        double? normalizedDailyTarget = EnsureNonNegativeFinite(dailyCalorieTarget, nameof(DailyCalorieTarget));
        double? normalizedMonday = EnsureNonNegativeFinite(mondayCalories, nameof(MondayCalories));
        double? normalizedTuesday = EnsureNonNegativeFinite(tuesdayCalories, nameof(TuesdayCalories));
        double? normalizedWednesday = EnsureNonNegativeFinite(wednesdayCalories, nameof(WednesdayCalories));
        double? normalizedThursday = EnsureNonNegativeFinite(thursdayCalories, nameof(ThursdayCalories));
        double? normalizedFriday = EnsureNonNegativeFinite(fridayCalories, nameof(FridayCalories));
        double? normalizedSaturday = EnsureNonNegativeFinite(saturdayCalories, nameof(SaturdayCalories));
        double? normalizedSunday = EnsureNonNegativeFinite(sundayCalories, nameof(SundayCalories));

        double weeklyTarget = calorieCyclingEnabled
            ? (normalizedMonday ?? normalizedDailyTarget ?? 0)
              + (normalizedTuesday ?? normalizedDailyTarget ?? 0)
              + (normalizedWednesday ?? normalizedDailyTarget ?? 0)
              + (normalizedThursday ?? normalizedDailyTarget ?? 0)
              + (normalizedFriday ?? normalizedDailyTarget ?? 0)
              + (normalizedSaturday ?? normalizedDailyTarget ?? 0)
              + (normalizedSunday ?? normalizedDailyTarget ?? 0)
            : (normalizedDailyTarget ?? 0) * 7;

        return !double.IsFinite(weeklyTarget)
            ? throw new ArgumentOutOfRangeException(
                nameof(dailyCalorieTarget),
                "Daily and per-day calorie targets must produce a finite weekly total.")
            : weeklyTarget;
    }

    private static double? EnsureNonNegativeFinite(double? value, string paramName) {
        if (value.HasValue && !double.IsFinite(value.Value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be a finite number.");
        }

        return value is < 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.")
            : value;
    }
}
