namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class ShoppingList {
        public static Error NotFound(Guid id) => new(
            "ShoppingList.NotFound",
            $"Shopping list with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error CurrentNotFound() => new(
            "ShoppingList.NotFound",
            "Shopping list was not found.",
            Kind: ErrorKind.NotFound);
    }
}
