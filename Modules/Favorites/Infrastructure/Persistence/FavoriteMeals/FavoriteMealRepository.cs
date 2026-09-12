using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;

public sealed class FavoriteMealRepository(FoodDiaryDbContext context, IFavoriteMealQuery queries) : IFavoriteMealRepository {
    public Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) {
        context.FavoriteMeals.Add(favorite);
        return Task.FromResult(favorite);
    }

    public Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) {
        context.FavoriteMeals.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<FavoriteMeal?> GetByIdAsync(
        FavoriteMealId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<FavoriteMeal> query = context.FavoriteMeals
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteMeal?> GetByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.MealId == mealId && f.UserId == userId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .AnyAsync(
                f => f.MealId == mealId && f.UserId == userId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default) {
        if (mealIds.Count == 0) {
            return new Dictionary<MealId, FavoriteMeal>();
        }

        List<FavoriteMeal> favorites = await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId && mealIds.Contains(f.MealId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(favorite => favorite.MealId);
    }

    public async Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
        UserId userId,
        IReadOnlyCollection<MealId> mealIds,
        CancellationToken cancellationToken = default) {
        if (mealIds.Count == 0) {
            return new Dictionary<MealId, FavoriteMealId>();
        }

        List<FavoriteMealIdByMealIdReadModel> favorites = await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId && mealIds.Contains(f.MealId))
            .Select(f => new FavoriteMealIdByMealIdReadModel(MealId: f.MealId, FavoriteMealId: f.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(favorite => favorite.MealId, favorite => favorite.FavoriteMealId);
    }

    public async Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) =>
        queries.GetAllReadModelsAsync(userId, cancellationToken);

    private sealed record FavoriteMealIdByMealIdReadModel(MealId MealId, FavoriteMealId FavoriteMealId);
}
