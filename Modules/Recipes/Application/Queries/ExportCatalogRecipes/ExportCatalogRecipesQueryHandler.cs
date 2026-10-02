using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;

public sealed class ExportCatalogRecipesQueryHandler(IRecipeOverviewReadService reader)
    : IQueryHandler<ExportCatalogRecipesQuery, Result<IReadOnlyList<CatalogRecipeModel>>> {
    public async Task<Result<IReadOnlyList<CatalogRecipeModel>>> Handle(ExportCatalogRecipesQuery query, CancellationToken cancellationToken) {
        var result = new List<CatalogRecipeModel>();
        for (int page = 1; ; page++) {
            (IReadOnlyList<RecipeOverviewReadItem> items, int total) = await reader.GetPagedAsync(
                UserId.Empty, includePublic: true, page, limit: 100, new RecipeQueryFilters(Search: null), cancellationToken).ConfigureAwait(false);
            if (total > 5000) {
                return Result.Failure<IReadOnlyList<CatalogRecipeModel>>(Errors.Validation.Invalid("Catalog", "Export supports at most 5000 recipes."));
            }
            result.AddRange(items.Select(item => new CatalogRecipeModel(
                item.Id.Value, item.Name, item.Description, item.Category, item.ImageUrl, item.PrepTime,
                item.CookTime, item.Servings, item.Language, item.LanguageConfirmed, item.IsNutritionAutoCalculated,
                item.ManualCalories, item.ManualProteins, item.ManualFats, item.ManualCarbs, item.ManualFiber, item.ManualAlcohol,
                item.Steps.Select(step => new RecipeStepInput(step.StepNumber, step.Instruction, step.Title, step.ImageUrl, ImageAssetId: null,
                    step.Ingredients.Select(ingredient => new RecipeIngredientInput(ingredient.ProductId, ingredient.NestedRecipeId, ingredient.Amount) {
                        TextName = ingredient.TextName,
                        AmountText = ingredient.AmountText,
                    }).ToArray())).ToArray())));
            if (result.Count >= total || items.Count == 0) {
                return Result.Success<IReadOnlyList<CatalogRecipeModel>>(result);
            }
        }
    }
}
