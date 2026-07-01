using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardMealItemsLoader(FoodDiaryDbContext context) {
    public async Task<ILookup<MealId, DashboardMealItemReadModel>> LoadAsync(
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<DashboardMealItemProjection> items = await context.MealItems
            .AsNoTracking()
            .Where(item => mealIds.Contains(item.MealId))
            .Select(item => new DashboardMealItemProjection(
                item.MealId,
                item.Id.Value,
                item.MealId.Value,
                item.Amount,
                item.ProductId.HasValue ? item.ProductId.Value.Value : null,
                item.SnapshotName,
                item.SnapshotImageUrl,
                item.SnapshotUnit,
                item.SnapshotBaseAmount,
                item.SnapshotCaloriesPerBase,
                item.SnapshotProteinsPerBase,
                item.SnapshotFatsPerBase,
                item.SnapshotCarbsPerBase,
                item.SnapshotFiberPerBase,
                item.SnapshotAlcoholPerBase,
                item.Product == null ? null : item.Product.Name,
                item.Product == null ? null : item.Product.ImageUrl,
                item.Product == null ? null : item.Product.BaseUnit.ToString(),
                item.Product == null ? null : item.Product.BaseAmount,
                item.Product == null ? null : item.Product.CaloriesPerBase,
                item.Product == null ? null : item.Product.ProteinsPerBase,
                item.Product == null ? null : item.Product.FatsPerBase,
                item.Product == null ? null : item.Product.CarbsPerBase,
                item.Product == null ? null : item.Product.FiberPerBase,
                item.Product == null ? null : item.Product.AlcoholPerBase,
                item.Product == null ? null : item.Product.ProductType,
                item.RecipeId.HasValue ? item.RecipeId.Value.Value : null,
                item.Recipe == null ? null : item.Recipe.Name,
                item.Recipe == null ? null : item.Recipe.ImageUrl,
                item.Recipe == null ? null : item.Recipe.Servings,
                item.Recipe == null ? null : item.Recipe.TotalCalories,
                item.Recipe == null ? null : item.Recipe.TotalProteins,
                item.Recipe == null ? null : item.Recipe.TotalFats,
                item.Recipe == null ? null : item.Recipe.TotalCarbs,
                item.Recipe == null ? null : item.Recipe.TotalFiber,
                item.Recipe == null ? null : item.Recipe.TotalAlcohol,
                item.SourceAiItemId.HasValue ? item.SourceAiItemId.Value.Value : null,
                item.Origin.ToString()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items
            .OrderBy(item => item.ItemId)
            .Select(ToReadModel)
            .ToLookup(item => new MealId(item.MealId));
    }

    private static DashboardMealItemReadModel ToReadModel(DashboardMealItemProjection item) {
        FoodQualityScore? productQuality = item.ProductCaloriesPerBase is null
            ? null
            : FoodQualityScore.Calculate(
                item.ProductCaloriesPerBase.Value,
                item.ProductProteinsPerBase ?? 0,
                item.ProductFatsPerBase ?? 0,
                item.ProductCarbsPerBase ?? 0,
                item.ProductFiberPerBase ?? 0,
                item.ProductAlcoholPerBase ?? 0,
                item.ProductType ?? ProductType.Unknown);
        bool hasNutritionSnapshot = item.SnapshotBaseAmount.HasValue
            && item.SnapshotCaloriesPerBase.HasValue
            && item.SnapshotProteinsPerBase.HasValue
            && item.SnapshotFatsPerBase.HasValue
            && item.SnapshotCarbsPerBase.HasValue
            && item.SnapshotFiberPerBase.HasValue
            && item.SnapshotAlcoholPerBase.HasValue;

        return new DashboardMealItemReadModel(
            item.ItemId,
            item.MealIdValue,
            item.Amount,
            item.ProductId,
            item.SnapshotName ?? item.ProductName,
            item.SnapshotImageUrl ?? item.ProductImageUrl,
            item.SnapshotUnit ?? item.ProductBaseUnit,
            item.SnapshotBaseAmount ?? item.ProductBaseAmount,
            item.SnapshotCaloriesPerBase ?? item.ProductCaloriesPerBase,
            item.SnapshotProteinsPerBase ?? item.ProductProteinsPerBase,
            item.SnapshotFatsPerBase ?? item.ProductFatsPerBase,
            item.SnapshotCarbsPerBase ?? item.ProductCarbsPerBase,
            item.SnapshotFiberPerBase ?? item.ProductFiberPerBase,
            item.SnapshotAlcoholPerBase ?? item.ProductAlcoholPerBase,
            productQuality?.Score,
            productQuality?.Grade.ToString().ToLowerInvariant(),
            item.RecipeId,
            item.SnapshotName ?? item.RecipeName,
            item.SnapshotImageUrl ?? item.RecipeImageUrl,
            hasNutritionSnapshot ? 1 : item.RecipeServings,
            item.SnapshotCaloriesPerBase ?? item.RecipeTotalCalories,
            item.SnapshotProteinsPerBase ?? item.RecipeTotalProteins,
            item.SnapshotFatsPerBase ?? item.RecipeTotalFats,
            item.SnapshotCarbsPerBase ?? item.RecipeTotalCarbs,
            item.SnapshotFiberPerBase ?? item.RecipeTotalFiber,
            item.SnapshotAlcoholPerBase ?? item.RecipeTotalAlcohol,
            item.SourceAiItemId,
            item.Origin);
    }
}
