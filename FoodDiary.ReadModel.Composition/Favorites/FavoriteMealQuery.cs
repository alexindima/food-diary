using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;

public sealed class FavoriteMealQuery(FoodDiaryDbContext context) : IFavoriteMealQuery {
    public async Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(PaginationPolicy.MaxCollectionSize)
            .Join(context.Meals.AsNoTracking(), favorite => favorite.MealId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source })
            .Select(row => new FavoriteMealReadModel(
                row.Favorite.Id.Value,
                row.Favorite.MealId.Value,
                row.Favorite.Name,
                row.Favorite.CreatedAtUtc,
                row.Source.Date,
                row.Source.MealType == null ? null : row.Source.MealType.ToString(),
                row.Source.TotalCalories,
                row.Source.TotalProteins,
                row.Source.TotalFats,
                row.Source.TotalCarbs,
                row.Source.Items.Count))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
