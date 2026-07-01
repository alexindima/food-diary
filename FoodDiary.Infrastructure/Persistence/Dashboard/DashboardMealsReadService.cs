using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Dashboard;

internal sealed class DashboardMealsReadService(FoodDiaryDbContext context) : IDashboardMealsReadService {
    private readonly DashboardMealFavoritesLoader favoriteMealsLoader = new(context);
    private readonly DashboardMealItemsLoader mealItemsLoader = new(context);
    private readonly DashboardMealAiSessionsLoader aiSessionsLoader = new(context);

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
        IQueryable<DashboardMealProjection> filteredMeals = CreateFilteredMealsQuery(userId, normalizedFrom, normalizedTo);

        int totalItems = await filteredMeals.CountAsync(cancellationToken).ConfigureAwait(false);
        List<DashboardMealProjection> meals = await filteredMeals
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (meals.Count == 0) {
            return Result.Success(new DashboardMealsReadModel([], pageNumber, pageSize, 0, totalItems));
        }

        MealId[] mealIds = [.. meals.Select(meal => meal.MealId)];
        IReadOnlyDictionary<MealId, Guid> favoriteIdsByMealId = await favoriteMealsLoader.LoadAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        ILookup<MealId, DashboardMealItemReadModel> itemsByMealId = await mealItemsLoader.LoadAsync(mealIds, cancellationToken).ConfigureAwait(false);
        ILookup<MealId, DashboardMealAiSessionReadModel> aiSessionsByMealId = await aiSessionsLoader.LoadAsync(mealIds, cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return Result.Success(new DashboardMealsReadModel(
            [.. meals.Select(meal => ToReadModel(meal, favoriteIdsByMealId, itemsByMealId, aiSessionsByMealId))],
            pageNumber,
            pageSize,
            totalPages,
            totalItems));
    }

    private IQueryable<DashboardMealProjection> CreateFilteredMealsQuery(UserId userId, DateTime normalizedFrom, DateTime normalizedTo) {
        return context.Meals
            .AsNoTracking()
            .Where(meal => meal.UserId == userId && meal.Date >= normalizedFrom && meal.Date <= normalizedTo)
            .OrderByDescending(meal => meal.Date)
            .ThenByDescending(meal => meal.CreatedOnUtc)
            .Select(meal => new DashboardMealProjection(
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

    private static DashboardMealReadModel ToReadModel(
        DashboardMealProjection meal,
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

    private static DateTime NormalizeUtcInstant(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
}
