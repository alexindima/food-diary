using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public static class RecipeErrors {
    public static Error NotFound(Guid id) => new(
        "Recipe.NotFound",
        $"Recipe with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error NotAccessible(Guid id) => new(
        "Recipe.NotAccessible",
        $"Recipe with ID {id} does not belong to the current user or was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidData(string message) => new(
        "Recipe.InvalidData",
        message,
        Kind: ErrorKind.Internal);
}
