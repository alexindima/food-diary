using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.ShoppingLists.Common;

public static class ShoppingListErrors {
    public static Error NotFound(Guid id) => new(
        "ShoppingList.NotFound",
        $"Shopping list with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error CurrentNotFound() => new(
        "ShoppingList.NotFound",
        "Shopping list was not found.",
        Kind: ErrorKind.NotFound);
}
