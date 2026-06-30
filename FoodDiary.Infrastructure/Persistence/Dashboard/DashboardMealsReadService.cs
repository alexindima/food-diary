using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardMealsReadService(FoodDiaryDbContext context) : IDashboardMealsReadService {
    public async Task<Result<DashboardMealsReadModel>> GetMealsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        if (dateFrom > dateTo) {
            return Result.Failure<DashboardMealsReadModel>(
                Errors.Validation.Invalid(nameof(dateFrom), "DateFrom must be earlier than DateTo"));
        }

        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Clamp(limit, 1, 100);
        DateTime normalizedFrom = NormalizeUtcInstant(dateFrom);
        DateTime normalizedTo = NormalizeUtcInstant(dateTo);
        IQueryable<MealProjection> filteredMeals = CreateFilteredMealsQuery(userId, normalizedFrom, normalizedTo);

        int totalItems = await filteredMeals.CountAsync(cancellationToken).ConfigureAwait(false);
        List<MealProjection> meals = await filteredMeals
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (meals.Count == 0) {
            return Result.Success(new DashboardMealsReadModel([], pageNumber, pageSize, 0, totalItems));
        }

        MealId[] mealIds = [.. meals.Select(meal => meal.MealId)];
        IReadOnlyDictionary<MealId, Guid> favoriteIdsByMealId = await LoadFavoriteIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        ILookup<MealId, DashboardMealItemReadModel> itemsByMealId = await LoadItemsAsync(mealIds, cancellationToken).ConfigureAwait(false);
        ILookup<MealId, DashboardMealAiSessionReadModel> aiSessionsByMealId = await LoadAiSessionsAsync(mealIds, cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return Result.Success(new DashboardMealsReadModel(
            [.. meals.Select(meal => ToReadModel(meal, favoriteIdsByMealId, itemsByMealId, aiSessionsByMealId))],
            pageNumber,
            pageSize,
            totalPages,
            totalItems));
    }

    private IQueryable<MealProjection> CreateFilteredMealsQuery(UserId userId, DateTime normalizedFrom, DateTime normalizedTo) {
        return context.Meals
            .AsNoTracking()
            .Where(meal => meal.UserId == userId && meal.Date >= normalizedFrom && meal.Date <= normalizedTo)
            .OrderByDescending(meal => meal.Date)
            .ThenByDescending(meal => meal.CreatedOnUtc)
            .Select(meal => new MealProjection(
                meal.Id,
                meal.Id.Value,
                meal.Date,
                meal.MealType.HasValue ? meal.MealType.Value.ToString() : null,
                meal.Comment,
                meal.ImageUrl,
                meal.ImageAssetId.HasValue ? meal.ImageAssetId.Value.Value : null,
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
                meal.PostMealSatietyLevel));
    }

    private async Task<IReadOnlyDictionary<MealId, Guid>> LoadFavoriteIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<FavoriteProjection> favorites = await context.FavoriteMeals
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId && mealIds.Contains(favorite.MealId))
            .Select(favorite => new FavoriteProjection(favorite.MealId, favorite.Id.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(favorite => favorite.MealId, favorite => favorite.FavoriteMealId);
    }

    private async Task<ILookup<MealId, DashboardMealItemReadModel>> LoadItemsAsync(
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<MealItemProjection> items = await context.MealItems
            .AsNoTracking()
            .Where(item => mealIds.Contains(item.MealId))
            .Select(item => new MealItemProjection(
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

    private async Task<ILookup<MealId, DashboardMealAiSessionReadModel>> LoadAiSessionsAsync(
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken) {
        List<MealAiSessionProjection> sessions = await context.MealAiSessions
            .AsNoTracking()
            .Where(session => mealIds.Contains(session.MealId))
            .Select(session => new MealAiSessionProjection(
                session.MealId,
                session.Id,
                session.Id.Value,
                session.MealId.Value,
                session.ImageAssetId.HasValue ? session.ImageAssetId.Value.Value : null,
                session.ImageAsset == null ? null : session.ImageAsset.Url,
                session.Source.ToString(),
                session.Status.ToString(),
                session.RecognizedAtUtc,
                session.Notes))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        MealAiSessionId[] sessionIds = [.. sessions.Select(session => session.SessionId)];
        ILookup<MealAiSessionId, DashboardMealAiItemReadModel> itemsBySessionId = await LoadAiItemsAsync(sessionIds, cancellationToken).ConfigureAwait(false);

        return sessions
            .OrderBy(session => session.RecognizedAtUtc)
            .Select(session => ToReadModel(session, itemsBySessionId))
            .ToLookup(session => new MealId(session.MealId));
    }

    private async Task<ILookup<MealAiSessionId, DashboardMealAiItemReadModel>> LoadAiItemsAsync(
        IReadOnlyCollection<MealAiSessionId> sessionIds,
        CancellationToken cancellationToken) {
        if (sessionIds.Count == 0) {
            return Array.Empty<DashboardMealAiItemReadModel>().ToLookup(item => new MealAiSessionId(item.SessionId));
        }

        List<DashboardMealAiItemReadModel> items = await context.MealAiItems
            .AsNoTracking()
            .Where(item => sessionIds.Contains(item.MealAiSessionId))
            .OrderBy(item => item.Id)
            .Select(item => new DashboardMealAiItemReadModel(
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
                item.Resolution.ToString()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.ToLookup(item => new MealAiSessionId(item.SessionId));
    }

    private static DashboardMealReadModel ToReadModel(
        MealProjection meal,
        IReadOnlyDictionary<MealId, Guid> favoriteIdsByMealId,
        ILookup<MealId, DashboardMealItemReadModel> itemsByMealId,
        ILookup<MealId, DashboardMealAiSessionReadModel> aiSessionsByMealId) {
        bool isFavorite = favoriteIdsByMealId.TryGetValue(meal.MealId, out Guid favoriteMealId);
        return new DashboardMealReadModel(
            meal.Id,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId,
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
            isFavorite,
            isFavorite ? favoriteMealId : null,
            [.. itemsByMealId[meal.MealId]],
            [.. aiSessionsByMealId[meal.MealId]]);
    }

    private static DashboardMealItemReadModel ToReadModel(MealItemProjection item) {
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

    private static DashboardMealAiSessionReadModel ToReadModel(
        MealAiSessionProjection session,
        ILookup<MealAiSessionId, DashboardMealAiItemReadModel> itemsBySessionId) {
        return new DashboardMealAiSessionReadModel(
            session.Id,
            session.MealIdValue,
            session.ImageAssetId,
            session.ImageUrl,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. itemsBySessionId[session.SessionId]]);
    }

    private static DateTime NormalizeUtcInstant(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private sealed record MealProjection(
        MealId MealId,
        Guid Id,
        DateTime Date,
        string? MealType,
        string? Comment,
        string? ImageUrl,
        Guid? ImageAssetId,
        double TotalCalories,
        double TotalProteins,
        double TotalFats,
        double TotalCarbs,
        double TotalFiber,
        double TotalAlcohol,
        bool IsNutritionAutoCalculated,
        double? ManualCalories,
        double? ManualProteins,
        double? ManualFats,
        double? ManualCarbs,
        double? ManualFiber,
        double? ManualAlcohol,
        int PreMealSatietyLevel,
        int PostMealSatietyLevel);

    private sealed record FavoriteProjection(MealId MealId, Guid FavoriteMealId);

    private sealed record MealItemProjection(
        MealId MealId,
        Guid ItemId,
        Guid MealIdValue,
        double Amount,
        Guid? ProductId,
        string? SnapshotName,
        string? SnapshotImageUrl,
        string? SnapshotUnit,
        double? SnapshotBaseAmount,
        double? SnapshotCaloriesPerBase,
        double? SnapshotProteinsPerBase,
        double? SnapshotFatsPerBase,
        double? SnapshotCarbsPerBase,
        double? SnapshotFiberPerBase,
        double? SnapshotAlcoholPerBase,
        string? ProductName,
        string? ProductImageUrl,
        string? ProductBaseUnit,
        double? ProductBaseAmount,
        double? ProductCaloriesPerBase,
        double? ProductProteinsPerBase,
        double? ProductFatsPerBase,
        double? ProductCarbsPerBase,
        double? ProductFiberPerBase,
        double? ProductAlcoholPerBase,
        ProductType? ProductType,
        Guid? RecipeId,
        string? RecipeName,
        string? RecipeImageUrl,
        int? RecipeServings,
        double? RecipeTotalCalories,
        double? RecipeTotalProteins,
        double? RecipeTotalFats,
        double? RecipeTotalCarbs,
        double? RecipeTotalFiber,
        double? RecipeTotalAlcohol,
        Guid? SourceAiItemId,
        string Origin);

    private sealed record MealAiSessionProjection(
        MealId MealId,
        MealAiSessionId SessionId,
        Guid Id,
        Guid MealIdValue,
        Guid? ImageAssetId,
        string? ImageUrl,
        string Source,
        string Status,
        DateTime RecognizedAtUtc,
        string? Notes);
}
