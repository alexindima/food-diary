namespace FoodDiary.Application.Common.Nutrition;

public static class ManualNutritionLimits {
    public const double MaxCalories = 100_000;
    public const double MaxNutrient = 10_000;
    public const string MaxCaloriesErrorMessage = "Value must be less than or equal to 100000.";
    public const string MaxNutrientErrorMessage = "Value must be less than or equal to 10000.";
}
