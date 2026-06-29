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

    public double GetWeeklyTarget() {
        if (!CalorieCyclingEnabled) {
            return (DailyCalorieTarget ?? 0) * 7;
        }

        return (MondayCalories ?? DailyCalorieTarget ?? 0)
               + (TuesdayCalories ?? DailyCalorieTarget ?? 0)
               + (WednesdayCalories ?? DailyCalorieTarget ?? 0)
               + (ThursdayCalories ?? DailyCalorieTarget ?? 0)
               + (FridayCalories ?? DailyCalorieTarget ?? 0)
               + (SaturdayCalories ?? DailyCalorieTarget ?? 0)
               + (SundayCalories ?? DailyCalorieTarget ?? 0);
    }
}
