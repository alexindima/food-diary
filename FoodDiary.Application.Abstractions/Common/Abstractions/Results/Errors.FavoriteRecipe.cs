using FoodDiary.Results;

using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class FavoriteRecipe {
        public static Error NotFound(Guid id) => FavoriteRecipeErrors.NotFound(id);

        public static Error AlreadyExists => FavoriteRecipeErrors.AlreadyExists;
    }
}
