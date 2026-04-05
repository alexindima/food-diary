namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserGoalState(
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    double? WaterGoal,
    double? DesiredWeight,
    double? DesiredWaist,
    bool CalorieCyclingEnabled = false,
    double? MondayCalories = null,
    double? TuesdayCalories = null,
    double? WednesdayCalories = null,
    double? ThursdayCalories = null,
    double? FridayCalories = null,
    double? SaturdayCalories = null,
    double? SundayCalories = null) {
    public static UserGoalState CreateInitial() {
        return new UserGoalState(
            DailyCalorieTarget: null,
            ProteinTarget: null,
            FatTarget: null,
            CarbTarget: null,
            FiberTarget: null,
            WaterGoal: null,
            DesiredWeight: null,
            DesiredWaist: null);
    }
}
