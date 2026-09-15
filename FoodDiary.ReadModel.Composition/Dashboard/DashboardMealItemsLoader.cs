using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

internal sealed class DashboardMealItemsLoader(IMealItemDisplayReadService reader) {
    public async Task<ILookup<MealId, DashboardMealItemReadModel>> LoadAsync(
        UserId userId, IReadOnlyCollection<MealId> mealIds, CancellationToken cancellationToken) {
        IReadOnlyList<MealItemDisplayReadModel> items = await reader.GetByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        return items.Select(item => new DashboardMealItemReadModel(
            item.Id,
            item.MealId,
            item.Amount,
            item.ProductId,
            item.ProductName,
            item.ProductImageUrl,
            item.ProductBaseUnit,
            item.ProductBaseAmount,
            item.ProductCaloriesPerBase,
            item.ProductProteinsPerBase,
            item.ProductFatsPerBase,
            item.ProductCarbsPerBase,
            item.ProductFiberPerBase,
            item.ProductAlcoholPerBase,
            item.ProductQualityScore,
            item.ProductQualityGrade,
            item.RecipeId,
            item.RecipeName,
            item.RecipeImageUrl,
            item.RecipeServings,
            item.RecipeTotalCalories,
            item.RecipeTotalProteins,
            item.RecipeTotalFats,
            item.RecipeTotalCarbs,
            item.RecipeTotalFiber,
            item.RecipeTotalAlcohol,
            item.SourceAiItemId,
            item.Origin))
            .ToLookup(item => new MealId(item.MealId));
    }
}
