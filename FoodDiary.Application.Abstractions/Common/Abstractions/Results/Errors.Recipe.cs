using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Recipes.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Recipe {
        public static Error NotFound(Guid id) => RecipeErrors.NotFound(id);

        public static Error NotAccessible(Guid id) => RecipeErrors.NotAccessible(id);

        public static Error InvalidData(string message) => RecipeErrors.InvalidData(message);
    }
}
