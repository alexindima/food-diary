using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class MealProductNutritionQuery(FoodDiaryDbContext context) : IMealProductNutritionQuery {
    private static DateTime StartOfUtcDay(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    private static DateTime EndOfUtcDay(DateTime value) => DateTime.SpecifyKind(TemporalRangePolicy.GetInclusiveDayEnd(value), DateTimeKind.Utc);
    public async Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        DateTime from = StartOfUtcDay(date);
        DateTime toInclusive = EndOfUtcDay(date);

        return await context.Set<MealItem>()
            .AsNoTracking()
            .Where(item =>
                item.Meal.UserId == userId &&
                item.Meal.Date >= from &&
                item.Meal.Date <= toInclusive &&
                item.ProductId != null)
            .OrderBy(static item => item.Id)
            .Take(limit)
            .Select(item => new UsdaMealProductNutritionReadModel(
                item.Amount,
                context.Products.AsNoTracking().Where(product => product.Id == item.ProductId).Select(product => product.BaseAmount).Single(),
                context.Products.AsNoTracking().Where(product => product.Id == item.ProductId).Select(product => product.UsdaFdcId).Single()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
