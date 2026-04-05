using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.FavoriteMeals;

public class FavoriteMealRepository(FoodDiaryDbContext context) : IFavoriteMealRepository {
    public async Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) {
        context.FavoriteMeals.Add(favorite);
        await context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) {
        context.FavoriteMeals.Remove(favorite);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FavoriteMeal?> GetByIdAsync(
        FavoriteMealId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = context.FavoriteMeals
            .Include(f => f.Meal)
            .ThenInclude(m => m.Items)
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken);
    }

    public async Task<FavoriteMeal?> GetByMealIdAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.MealId == mealId && f.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteMeals
            .AsNoTracking()
            .Include(f => f.Meal)
            .ThenInclude(m => m.Items)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
