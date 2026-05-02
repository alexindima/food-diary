using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public class MealRepository(FoodDiaryDbContext context) : IMealRepository {
    private static DateTime StartOfUtcDay(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime StartOfNextUtcDay(DateTime value) =>
        DateTime.SpecifyKind(value.Date.AddDays(1), DateTimeKind.Utc);

    public async Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
        await context.Meals.AddAsync(meal, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return meal;
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) {
        context.Meals.Update(meal);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) {
        var tracked = await context.Meals.FindAsync([meal.Id], cancellationToken);
        if (tracked is not null) {
            context.Meals.Remove(tracked);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Meal?> GetByIdAsync(
        MealId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<Meal> query = context.Meals;

        if (includeItems) {
            query = query
                .AsSplitQuery()
                .Include(m => m.Items)
                .ThenInclude(i => i.Product)
                .Include(m => m.Items)
                .ThenInclude(i => i.Recipe)
                .Include(m => m.AiSessions)
                .ThenInclude(s => s.Items)
                .Include(m => m.AiSessions)
                .ThenInclude(s => s.ImageAsset);
        }

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            m => m.Id == id && m.UserId == userId,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default) {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        var filteredQuery = context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        if (dateFrom.HasValue) {
            var from = StartOfUtcDay(dateFrom.Value);
            filteredQuery = filteredQuery.Where(m => m.Date >= from);
        }

        if (dateTo.HasValue) {
            var toExclusive = StartOfNextUtcDay(dateTo.Value);
            filteredQuery = filteredQuery.Where(m => m.Date < toExclusive);
        }

        var totalItems = await filteredQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var itemsQuery = filteredQuery
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(i => i.Product)
            .Include(m => m.Items)
            .ThenInclude(i => i.Recipe)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.Items)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.ImageAsset)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedOnUtc);

        var items = await itemsQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        var from = StartOfUtcDay(dateFrom);
        var toExclusive = StartOfNextUtcDay(dateTo);

        return await context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        var from = StartOfUtcDay(dateFrom);
        var toExclusive = StartOfNextUtcDay(dateTo);

        return await context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .Select(m => m.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalMealCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.Meals
            .AsNoTracking()
            .CountAsync(m => m.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        var from = StartOfUtcDay(date);
        var toExclusive = StartOfNextUtcDay(date);

        return await context.Meals
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(i => i.Product)
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .ToListAsync(cancellationToken);
    }
}
