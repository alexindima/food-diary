using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Consumptions.Services;

public class MealNutritionService(
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService) : IMealNutritionService {

    public async Task<Result<MealNutritionSummary>> CalculateAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken = default) {
        var productIds = meal.Items
            .Where(i => i.ProductId.HasValue)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToList();

        var recipeIds = meal.Items
            .Where(i => i.RecipeId.HasValue)
            .Select(i => i.RecipeId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<ProductId, Product> products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken).ConfigureAwait(false);
        if (products.Count != productIds.Count) {
            ProductId missingProduct = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Product.NotAccessible(missingProduct.Value));
        }

        IReadOnlyDictionary<RecipeId, Recipe> recipes = await recipeLookupService.GetAccessibleByIdsAsync(recipeIds, userId, cancellationToken).ConfigureAwait(false);
        if (recipes.Count != recipeIds.Count) {
            RecipeId missingRecipe = recipeIds.First(id => !recipes.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Recipe.NotAccessible(missingRecipe.Value));
        }

        MealNutritionSummary summary = MealNutritionCalculator.Calculate(meal, products, recipes);
        return Result.Success(summary);
    }
}
