using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;

public sealed class FavoriteMealQuery(ICompositionReadContext context) : IFavoriteMealQuery {
    public async Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default) =>
        await ReadItemsAsync(userId, PaginationPolicy.MaxCollectionSize, cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetOverviewReadModelsAsync(
        UserId userId, int limit, CancellationToken cancellationToken = default) {
        int totalItems = await context.FavoriteMeals.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(context.Meals.AsNoTracking(), favorite => favorite.MealId, source => source.Id,
                (favorite, source) => favorite.Id)
            .CountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FavoriteMealReadModel> items = await ReadItemsAsync(
            userId, Math.Clamp(limit, 0, PaginationPolicy.MaxCollectionSize), cancellationToken).ConfigureAwait(false);
        return (items, totalItems);
    }

    private async Task<IReadOnlyList<FavoriteMealReadModel>> ReadItemsAsync(
        UserId userId, int limit, CancellationToken cancellationToken) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ThenByDescending(f => f.Id)
            .Take(limit)
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
