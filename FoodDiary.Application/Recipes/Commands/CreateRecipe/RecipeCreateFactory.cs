using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

internal static class RecipeCreateFactory {
    public static Recipe Create(CreateRecipeCommand command, CreateRecipeValues values) =>
        Recipe.Create(
            values.UserId,
            command.Name,
            command.Servings,
            command.Description,
            command.Comment,
            command.Category,
            values.ImageAsset?.Url ?? command.ImageUrl,
            values.ImageAssetId,
            command.PrepTime ?? 0,
            command.CookTime,
            values.Visibility);
}
