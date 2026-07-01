namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteMeal {
        public static Error NotFound(Guid id) => new(
            "FavoriteMeal.NotFound",
            $"Favorite meal with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteMeal.AlreadyExists",
            "This meal is already in favorites.",
            Kind: ErrorKind.Conflict);
    }
}
