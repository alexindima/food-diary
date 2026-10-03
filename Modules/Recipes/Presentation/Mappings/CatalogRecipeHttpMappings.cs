using FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Modules.Recipes.Presentation.Responses;

namespace FoodDiary.Modules.Recipes.Presentation.Mappings;

public static class CatalogRecipeHttpMappings {
    public static ExportCatalogRecipesQuery ToExportQuery() => new();

    extension(CatalogRecipeHttpRequest request) {
        public ImportCatalogRecipeCommand ToImportCommand(Guid userId, bool preview) =>
            new(userId, new CatalogRecipeModel(
                request.Id, request.Name, request.Description, request.Category, request.ImageUrl,
                request.PrepTime, request.CookTime, request.Servings, request.Language,
                request.LanguageConfirmed, request.CalculateNutritionAutomatically,
                request.ManualCalories, request.ManualProteins, request.ManualFats,
                request.ManualCarbs, request.ManualFiber, request.ManualAlcohol,
                request.Steps?.Select(step => step is null ? null! : step.ToInput()).ToList()!), preview);
    }

    extension(CatalogRecipeModel model) {
        public CatalogRecipeExportHttpResponse ToHttpResponse() =>
            new(model.Id, model.Name, model.Description, model.Category, model.ImageUrl,
                model.PrepTime, model.CookTime, model.Servings, model.Language,
                model.LanguageConfirmed, model.CalculateNutritionAutomatically,
                model.ManualCalories, model.ManualProteins, model.ManualFats,
                model.ManualCarbs, model.ManualFiber, model.ManualAlcohol,
                model.Steps.Select(step => step.ToHttpResponse()).ToList());
    }

    extension(CatalogRecipeImportResult result) {
        public CatalogRecipeImportHttpResponse ToHttpResponse() =>
            new(result.Id, result.Status, result.Errors);
    }

    extension(CatalogRecipeStepHttpRequest step) {
        private RecipeStepInput ToInput() =>
            new(step.Order, step.Description, step.Title, step.ImageUrl, step.ImageAssetId,
                step.Ingredients?.Select(ingredient => ingredient is null ? null! : ingredient.ToInput()).ToList()!) {
                ImageAssetIds = step.ImageAssetIds,
            };
    }

    extension(CatalogRecipeIngredientHttpRequest ingredient) {
        private RecipeIngredientInput ToInput() =>
            new(ingredient.ProductId, ingredient.NestedRecipeId, ingredient.Amount) {
                PublicName = ingredient.PublicName,
                PublicUnit = ingredient.PublicUnit,
                TextName = ingredient.TextName,
                AmountText = ingredient.AmountText,
            };
    }

    extension(RecipeStepInput step) {
        private CatalogRecipeStepHttpResponse ToHttpResponse() =>
            new(step.Order, step.Description, step.Title, step.ImageUrl, step.ImageAssetId,
                step.Ingredients.Select(ingredient => ingredient.ToHttpResponse()).ToList()) {
                ImageAssetIds = step.ImageAssetIds,
            };
    }

    extension(RecipeIngredientInput ingredient) {
        private CatalogRecipeIngredientHttpResponse ToHttpResponse() =>
            new(ingredient.ProductId, ingredient.NestedRecipeId, ingredient.Amount) {
                PublicName = ingredient.PublicName,
                PublicUnit = ingredient.PublicUnit,
                TextName = ingredient.TextName,
                AmountText = ingredient.AmountText,
            };
    }
}
