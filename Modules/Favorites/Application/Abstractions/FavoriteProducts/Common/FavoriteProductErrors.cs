using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.FavoriteProducts.Common;

public static class FavoriteProductErrors {
    public static Error NotFound(Guid id) => new(
        "FavoriteProduct.NotFound",
        $"Favorite product with id '{id}' was not found.",
        Kind: ErrorKind.NotFound);

    public static Error AlreadyExists => new(
        "FavoriteProduct.AlreadyExists",
        "This product is already in favorites.",
        Kind: ErrorKind.Conflict);
}
