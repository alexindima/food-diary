using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Dashboard;

internal sealed class DashboardMealsReadService(ICompositionReadContext context, IMealItemDisplayReadService mealItems) : IDashboardMealsReadService {
    private readonly DashboardMealItemsLoader _mealItemsLoader = new(mealItems);
    private readonly DashboardMealAiSessionsLoader _aiSessionsLoader = new(context);

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

        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
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
        ILookup<MealId, DashboardMealItemReadModel> itemsByMealId = await _mealItemsLoader.LoadAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        ILookup<MealId, DashboardMealAiSessionReadModel> aiSessionsByMealId = await _aiSessionsLoader.LoadAsync(mealIds, cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return Result.Success(new DashboardMealsReadModel(
            [.. meals.Select(meal => ToReadModel(meal, itemsByMealId, aiSessionsByMealId))],
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
            .ThenByDescending(meal => meal.Id)
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
                meal.PostMealSatietyLevel,
                context.FavoriteMeals.AsNoTracking().Where(favorite => favorite.UserId == userId && favorite.MealId == meal.Id)
                    .Select(favorite => (Guid?)favorite.Id.Value).FirstOrDefault()));
    }

    private static DashboardMealReadModel ToReadModel(
        DashboardMealProjection meal,
        ILookup<MealId, DashboardMealItemReadModel> itemsByMealId,
        ILookup<MealId, DashboardMealAiSessionReadModel> aiSessionsByMealId) {
        bool isFavorite = meal.FavoriteMealId.HasValue;
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
            meal.FavoriteMealId,
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
