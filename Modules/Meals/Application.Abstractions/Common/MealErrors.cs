using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public static class MealErrors {
    public static Error NotFound(Guid id) => new(
        "Meal.NotFound",
        $"Meal with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidData(string message) => new(
        "Meal.InvalidData",
        message,
        Kind: ErrorKind.Internal);
}
