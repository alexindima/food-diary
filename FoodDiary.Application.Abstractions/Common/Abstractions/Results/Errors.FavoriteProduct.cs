namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteProduct {
        public static Error NotFound(Guid id) => new(
            "FavoriteProduct.NotFound",
            $"Favorite product with id '{id}' was not found.",
            Kind: ErrorKind.NotFound);

        public static Error AlreadyExists => new(
            "FavoriteProduct.AlreadyExists",
            "This product is already in favorites.",
            Kind: ErrorKind.Conflict);
    }
}
