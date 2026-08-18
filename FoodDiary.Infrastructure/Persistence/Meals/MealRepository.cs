using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class MealRepository(FoodDiaryDbContext context) : IMealRepository {
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
            query = IncludeMealGraph(query);
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
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        IQueryable<Meal> filteredQuery = context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        filteredQuery = ApplyFilters(filteredQuery, filters);

        int totalItems = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        int skip = (pageNumber - 1) * pageSize;

        IOrderedQueryable<Meal> itemsQuery = IncludeMealGraph(filteredQuery)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedOnUtc);

        List<Meal> items = await itemsQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, totalItems);
    }

    public async Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        IQueryable<Meal> filteredQuery = context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        filteredQuery = ApplyFilters(filteredQuery, filters);

        int totalItems = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        int skip = (pageNumber - 1) * pageSize;

        List<Meal> meals = await IncludeMealGraph(filteredQuery)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedOnUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return ([.. meals.Select(ToMealProjectionReadModel)], totalItems);
    }

    public async Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
        MealId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        Meal? meal = await IncludeMealGraph(context.Meals.AsNoTracking())
            .Where(m => m.Id == id && m.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return meal is null ? null : ToMealProjectionReadModel(meal);
    }

    public async Task<int> GetCountAsync(
        UserId userId,
        MealQueryFilters filters,
        CancellationToken cancellationToken = default) {
        IQueryable<Meal> filteredQuery = context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId);

        return await ApplyFilters(filteredQuery, filters)
            .CountAsync(cancellationToken).ConfigureAwait(false);
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

    private static IQueryable<Meal> IncludeMealGraph(IQueryable<Meal> query) =>
        query
            .AsSplitQuery()
            .Include(m => m.Items)
            .ThenInclude(i => i.Product)
            .Include(m => m.Items)
            .ThenInclude(i => i.Recipe)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.Items)
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.ImageAsset);

    public async Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(dateFrom);
        DateTime toExclusive = StartOfNextUtcDay(dateTo);

        return await IncludeMealGraph(context.Meals.AsNoTracking())
            .Where(m => m.UserId == userId && m.Date >= from && m.Date < toExclusive)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<Meal> meals = await GetByPeriodAsync(userId, dateFrom, dateTo, cancellationToken).ConfigureAwait(false);
        return [.. meals.Select(ToMealProjectionReadModel)];
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

    public async Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(date);
        DateTime toExclusive = StartOfNextUtcDay(date);

        return await context.Set<MealItem>()
            .AsNoTracking()
            .Where(item =>
                item.Meal.UserId == userId &&
                item.Meal.Date >= from &&
                item.Meal.Date < toExclusive &&
                item.ProductId != null &&
                item.Product != null)
            .Select(item => new MealProductNutritionReadModel(
                item.Amount,
                item.Product!.BaseAmount,
                item.Product.UsdaFdcId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MealProjectionReadModel ToMealProjectionReadModel(Meal meal) {
        return new MealProjectionReadModel(
            meal.Id.Value,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
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
            ToMealItemProjectionReadModels(meal),
            ToMealAiSessionProjectionReadModels(meal));
    }

    private static List<MealItemProjectionReadModel> ToMealItemProjectionReadModels(Meal meal) {
        return [.. meal.Items
            .OrderBy(static item => item.Id.Value)
            .Select(ToMealItemProjectionReadModel)];
    }

    private static MealItemProjectionReadModel ToMealItemProjectionReadModel(MealItem item) {
        return new MealItemProjectionReadModel(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.SnapshotName ?? item.Product?.Name,
            item.SnapshotImageUrl ?? item.Product?.ImageUrl,
            item.SnapshotUnit ?? item.Product?.BaseUnit.ToString(),
            item.SnapshotBaseAmount ?? item.Product?.BaseAmount,
            item.SnapshotCaloriesPerBase ?? item.Product?.CaloriesPerBase,
            item.SnapshotProteinsPerBase ?? item.Product?.ProteinsPerBase,
            item.SnapshotFatsPerBase ?? item.Product?.FatsPerBase,
            item.SnapshotCarbsPerBase ?? item.Product?.CarbsPerBase,
            item.SnapshotFiberPerBase ?? item.Product?.FiberPerBase,
            item.SnapshotAlcoholPerBase ?? item.Product?.AlcoholPerBase,
            item.Product?.ProductType,
            item.RecipeId?.Value,
            item.SnapshotName ?? item.Recipe?.Name,
            item.SnapshotImageUrl ?? item.Recipe?.ImageUrl,
            GetRecipeServings(item),
            item.SnapshotCaloriesPerBase ?? item.Recipe?.TotalCalories,
            item.SnapshotProteinsPerBase ?? item.Recipe?.TotalProteins,
            item.SnapshotFatsPerBase ?? item.Recipe?.TotalFats,
            item.SnapshotCarbsPerBase ?? item.Recipe?.TotalCarbs,
            item.SnapshotFiberPerBase ?? item.Recipe?.TotalFiber,
            item.SnapshotAlcoholPerBase ?? item.Recipe?.TotalAlcohol,
            item.SourceAiItemId?.Value,
            item.Origin);
    }

    private static int? GetRecipeServings(MealItem item) {
        if (item.HasNutritionSnapshot) {
            return 1;
        }

        return item.Recipe?.Servings;
    }

    private static List<MealAiSessionProjectionReadModel> ToMealAiSessionProjectionReadModels(Meal meal) {
        return [.. meal.AiSessions
            .OrderBy(static session => session.RecognizedAtUtc)
            .Select(ToMealAiSessionProjectionReadModel)];
    }

    private static MealAiSessionProjectionReadModel ToMealAiSessionProjectionReadModel(MealAiSession session) {
        return new MealAiSessionProjectionReadModel(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            session.ImageAsset?.Url,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items
                .OrderBy(static item => item.Id.Value)
                .Select(ToMealAiItemProjectionReadModel)]);
    }

    private static MealAiItemProjectionReadModel ToMealAiItemProjectionReadModel(MealAiItem item) {
        return new MealAiItemProjectionReadModel(
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
            item.Resolution);
    }
}
