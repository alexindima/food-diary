using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealReadRepository {
    Task<Meal?> GetByIdAsync(
        MealId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default);

    async Task<(IReadOnlyList<MealConsumptionReadModel> Items, int TotalItems)> GetPagedConsumptionReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<Meal> items, int totalItems) = await GetPagedAsync(userId, page, limit, filters, cancellationToken).ConfigureAwait(false);
        return ([.. items.Select(ToConsumptionReadModel)], totalItems);
    }

    async Task<int> GetCountAsync(
        UserId userId,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        (_, int totalItems) = await GetPagedAsync(userId, page: 1, limit: 1, filters, cancellationToken).ConfigureAwait(false);
        return totalItems;
    }

    Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    async Task<MealConsumptionReadModel?> GetByIdConsumptionReadModelAsync(
        MealId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        Meal? meal = await GetByIdAsync(id, userId, includeItems: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return meal is null ? null : ToConsumptionReadModel(meal);
    }

    Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalMealCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<Meal> meals = await GetWithItemsAndProductsAsync(userId, date, cancellationToken).ConfigureAwait(false);

        return [
            .. meals
                .SelectMany(static meal => meal.Items)
                .Where(static item => item.IsProduct && item.Product is not null)
                .Select(static item => new MealProductNutritionReadModel(
                    item.Amount,
                    item.Product!.BaseAmount,
                    item.Product.UsdaFdcId)),
        ];
    }

    private static MealConsumptionReadModel ToConsumptionReadModel(Meal meal) {
        return new MealConsumptionReadModel(
            meal.Id.Value,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            meal.TotalAlcohol,
            meal.IsNutritionAutoCalculated,
            meal.ManualCalories,
            meal.ManualProteins,
            meal.ManualFats,
            meal.ManualCarbs,
            meal.ManualFiber,
            meal.ManualAlcohol,
            meal.PreMealSatietyLevel,
            meal.PostMealSatietyLevel,
            [.. meal.Items.Select(ToConsumptionItemReadModel)],
            [.. meal.AiSessions.Select(ToConsumptionAiSessionReadModel)]);
    }

    private static MealConsumptionItemReadModel ToConsumptionItemReadModel(MealItem item) {
        bool hasSnapshot = item.HasNutritionSnapshot;
        return new MealConsumptionItemReadModel(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.SnapshotName ?? item.Product?.Name,
            item.SnapshotImageUrl ?? item.Product?.ImageUrl,
            item.SnapshotUnit ?? item.Product?.BaseUnit.ToString(),
            item.SnapshotBaseAmount ?? item.Product?.BaseAmount,
            item.SnapshotCaloriesPerBase ?? item.Product?.CaloriesPerBase,
            item.SnapshotProteinsPerBase ?? item.Product?.ProteinsPerBase,
            item.SnapshotFatsPerBase ?? item.Product?.FatsPerBase,
            item.SnapshotCarbsPerBase ?? item.Product?.CarbsPerBase,
            item.SnapshotFiberPerBase ?? item.Product?.FiberPerBase,
            item.SnapshotAlcoholPerBase ?? item.Product?.AlcoholPerBase,
            item.Product?.ProductType,
            item.RecipeId?.Value,
            item.SnapshotName ?? item.Recipe?.Name,
            item.SnapshotImageUrl ?? item.Recipe?.ImageUrl,
            hasSnapshot ? 1 : item.Recipe?.Servings,
            item.SnapshotCaloriesPerBase ?? item.Recipe?.TotalCalories,
            item.SnapshotProteinsPerBase ?? item.Recipe?.TotalProteins,
            item.SnapshotFatsPerBase ?? item.Recipe?.TotalFats,
            item.SnapshotCarbsPerBase ?? item.Recipe?.TotalCarbs,
            item.SnapshotFiberPerBase ?? item.Recipe?.TotalFiber,
            item.SnapshotAlcoholPerBase ?? item.Recipe?.TotalAlcohol,
            item.SourceAiItemId?.Value,
            item.Origin);
    }

    private static MealConsumptionAiSessionReadModel ToConsumptionAiSessionReadModel(MealAiSession session) {
        return new MealConsumptionAiSessionReadModel(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            session.ImageAsset?.Url,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.Select(ToConsumptionAiItemReadModel)]);
    }

    private static MealConsumptionAiItemReadModel ToConsumptionAiItemReadModel(MealAiItem item) {
        return new MealConsumptionAiItemReadModel(
            item.Id.Value,
            item.MealAiSessionId.Value,
            item.NameEn,
            item.NameLocal,
            item.Amount,
            item.Unit,
            item.Calories,
            item.Proteins,
            item.Fats,
            item.Carbs,
            item.Fiber,
            item.Alcohol,
            item.Confidence,
            item.Resolution);
    }
}
