using FoodDiary.Results;

using FoodDiary.Application.Abstractions.ShoppingLists.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class ShoppingList {
        public static Error NotFound(Guid id) => ShoppingListErrors.NotFound(id);

        public static Error CurrentNotFound() => ShoppingListErrors.CurrentNotFound();
    }
}
