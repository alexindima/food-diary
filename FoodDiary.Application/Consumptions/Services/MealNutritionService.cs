using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Recipes.Common;

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

        var products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken);
        if (products.Count != productIds.Count) {
            var missingProduct = productIds.First(id => !products.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Product.NotAccessible(missingProduct.Value));
        }

        var recipes = await recipeLookupService.GetAccessibleByIdsAsync(recipeIds, userId, cancellationToken);
        if (recipes.Count != recipeIds.Count) {
            var missingRecipe = recipeIds.First(id => !recipes.ContainsKey(id));
            return Result.Failure<MealNutritionSummary>(Errors.Recipe.NotAccessible(missingRecipe.Value));
        }

        var summary = MealNutritionCalculator.Calculate(meal, products, recipes);
        return Result.Success(summary);
    }
}
