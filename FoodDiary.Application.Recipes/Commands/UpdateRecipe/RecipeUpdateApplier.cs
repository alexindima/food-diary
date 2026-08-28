using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

internal static class RecipeUpdateApplier {
    public static void Apply(Recipe recipe, UpdateRecipeCommand command, UpdateRecipeValues values) {
        recipe.UpdateIdentity(
            name: command.Name,
            description: command.Description,
            clearDescription: command.ClearDescription,
            comment: command.Comment,
            clearComment: command.ClearComment,
            category: command.Category,
            clearCategory: command.ClearCategory);
        recipe.UpdateMedia(
            imageUrl: values.ImageAsset?.Url ?? command.ImageUrl,
            clearImageUrl: values.ImageAsset is null && command.ClearImageUrl,
            imageAssetId: values.ImageAssetId,
            clearImageAssetId: command.ClearImageAssetId);
        recipe.UpdateTimingAndServings(
            prepTime: command.PrepTime,
            cookTime: command.CookTime,
            servings: command.Servings);

        if (values.Visibility.HasValue) {
            recipe.ChangeVisibility(values.Visibility.Value);
        }
    }
}
