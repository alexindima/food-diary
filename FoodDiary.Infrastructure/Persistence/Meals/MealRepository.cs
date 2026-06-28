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

    private static DateTime NormalizeUtcInstant(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private static DateTime NormalizeInclusiveEndInstant(DateTime value) {
        DateTime utc = NormalizeUtcInstant(value);
        return utc.TimeOfDay == TimeSpan.Zero
            ? DateTime.SpecifyKind(utc.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
            : utc;
    }

    public async Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
        await context.Meals.AddAsync(meal, cancellationToken).ConfigureAwait(false);
        return meal;
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) {
        context.Meals.Update(meal);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) {
        Meal? tracked = await context.Meals.FindAsync([meal.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.Meals.Remove(tracked);
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
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Meal> filteredQuery = context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        filteredQuery = ApplyFilters(filteredQuery, filters);

        int totalItems = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        int skip = (pageNumber - 1) * pageSize;

        IOrderedQueryable<Meal> itemsQuery = filteredQuery
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

        List<Meal> items = await itemsQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalItems);
    }

    private static IQueryable<Meal> ApplyFilters(IQueryable<Meal> query, MealQueryFilters filters) {
        if (filters.DateFrom.HasValue) {
            DateTime from = NormalizeUtcInstant(filters.DateFrom.Value);
            query = query.Where(m => m.Date >= from);
        }

        if (filters.DateTo.HasValue) {
            DateTime to = NormalizeInclusiveEndInstant(filters.DateTo.Value);
            query = query.Where(m => m.Date <= to);
        }

        if (filters.MealTypes is { Count: > 0 }) {
            query = query.Where(m => m.MealType.HasValue && filters.MealTypes.Contains(m.MealType.Value));
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.Where(m => (m.ManualCalories ?? m.TotalCalories) >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.Where(m => (m.ManualCalories ?? m.TotalCalories) <= filters.CaloriesTo.Value);
        }

        return ApplyPresenceFilters(query, filters);
    }

    private static IQueryable<Meal> ApplyPresenceFilters(IQueryable<Meal> query, MealQueryFilters filters) {
        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.Where(m => m.ImageUrl != null || m.ImageAssetId != null)
                : query.Where(m => m.ImageUrl == null && m.ImageAssetId == null);
        }

        if (filters.HasAiSession.HasValue) {
            query = filters.HasAiSession.Value
                ? query.Where(m => m.AiSessions.Any())
                : query.Where(m => !m.AiSessions.Any());
        }

        return query;
    }

    public async Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(dateFrom);
        DateTime toExclusive = StartOfNextUtcDay(dateTo);

        return await context.Meals
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(i => i.Product)
            .Include(m => m.Items)
            .ThenInclude(i => i.Recipe)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.Items)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.ImageAsset)
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(dateFrom);
        DateTime toExclusive = StartOfNextUtcDay(dateTo);

        return await context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .Select(m => m.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetTotalMealCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.Meals
            .AsNoTracking()
            .CountAsync(m => m.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(date);
        DateTime toExclusive = StartOfNextUtcDay(date);

        return await context.Meals
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(i => i.Product)
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
