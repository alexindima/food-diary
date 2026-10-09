using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;

internal static class RecipeUpdateApplier {
    public static void Apply(Recipe recipe, UpdateRecipeCommand command, UpdateRecipeValues values) {
        recipe.UpdateIdentityChanges(new RecipeIdentityChanges(
            command.Name,
            FieldChanges.FromOptionalText(command.Description, command.ClearDescription),
            FieldChanges.FromOptionalText(command.Comment, command.ClearComment),
            FieldChanges.FromOptionalText(command.Category, command.ClearCategory)));
        recipe.UpdateMediaChanges(new RecipeMediaChanges(
            FieldChanges.FromOptionalText(values.ImageAsset?.Url ?? command.ImageUrl,
                values.ImageAsset is null && command.ClearImageUrl),
            FieldChanges.FromOptionalValue(values.ImageAssetId, command.ClearImageAssetId)));
        recipe.UpdateTimingAndServings(
            prepTime: command.PrepTime,
            cookTime: command.CookTime,
            servings: command.Servings);

        if (command.Language is not null) { recipe.ChangeLanguage(command.Language); }

        recipe.SetLanguageConfirmation(command.LanguageConfirmed);
        if (values.Visibility.HasValue) {
            recipe.ChangeVisibility(values.Visibility.Value);
        }
    }
}
