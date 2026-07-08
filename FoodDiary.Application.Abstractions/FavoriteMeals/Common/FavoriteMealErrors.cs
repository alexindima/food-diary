using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.FavoriteMeals.Common;

public static class FavoriteMealErrors {
    public static Error NotFound(Guid id) => new(
        "FavoriteMeal.NotFound",
        $"Favorite meal with id '{id}' was not found.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyExists => new(
        "FavoriteMeal.AlreadyExists",
        "This meal is already in favorites.",
        Kind: ErrorKind.Conflict);
}
