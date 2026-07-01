namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteRecipe {
        public static Error NotFound(Guid id) => new(
            "FavoriteRecipe.NotFound",
            $"Favorite recipe with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteRecipe.AlreadyExists",
            "This recipe is already in favorites.",
            Kind: ErrorKind.Conflict);
    }
}
