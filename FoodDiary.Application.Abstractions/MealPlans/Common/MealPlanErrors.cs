using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.MealPlans.Common;

public static class MealPlanErrors {
    public static Error NotFound(Guid id) => new(
        "MealPlan.NotFound",
        $"Meal plan with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidId => new(
        "MealPlan.InvalidId",
        "Meal plan ID is required.",
        Kind: ErrorKind.Validation);

    public static Error NotCurated => new(
        "MealPlan.NotCurated",
        "Only curated meal plans can be adopted.",
        Kind: ErrorKind.Validation);
}
