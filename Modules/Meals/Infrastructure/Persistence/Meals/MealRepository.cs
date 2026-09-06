using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class MealRepository(FoodDiaryDbContext context) : IMealRepository {
    private static DateTime StartOfUtcDay(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime EndOfUtcDay(DateTime value) =>
        DateTime.SpecifyKind(TemporalRangePolicy.GetInclusiveDayEnd(value), DateTimeKind.Utc);

    private static DateTime NormalizeUtcInstant(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private static DateTime NormalizeInclusiveEndInstant(DateTime value) {
        DateTime utc = NormalizeUtcInstant(value);
        return utc.TimeOfDay == TimeSpan.Zero
            ? EndOfUtcDay(utc)
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

        IReadOnlyList<MealProjectionReadModel> projections = await ToMealProjectionReadModelsAsync(
            meals,
            cancellationToken).ConfigureAwait(false);
        return (projections, totalItems);
    }

    public async Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
        MealId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        Meal? meal = await IncludeMealGraph(context.Meals.AsNoTracking())
            .Where(m => m.Id == id && m.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (meal is null) {
            return null;
        }

        IReadOnlyList<MealProjectionReadModel> projections = await ToMealProjectionReadModelsAsync(
            [meal],
            cancellationToken).ConfigureAwait(false);
        return projections[0];
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
            .Include(m => m.AiSessions)
            .ThenInclude(s => s.Items);

    public async Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(dateFrom);
        DateTime toInclusive = EndOfUtcDay(dateTo);

        return await IncludeMealGraph(context.Meals.AsNoTracking())
            .Where(m => m.UserId == userId && m.Date >= from && m.Date <= toInclusive)
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
        return await ToMealProjectionReadModelsAsync(meals, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int limit,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        DateTime from = NormalizeUtcInstant(dateFrom);
        DateTime toInclusive = NormalizeUtcInstant(dateTo);

        List<Meal> meals = await IncludeMealGraph(context.Meals.AsNoTracking())
            .Where(meal => meal.UserId == userId && meal.Date >= from && meal.Date <= toInclusive)
            .OrderBy(meal => meal.Date)
            .ThenBy(meal => meal.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return await ToMealProjectionReadModelsAsync(meals, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default) {
        DateTime from = StartOfUtcDay(dateFrom);
        DateTime toInclusive = EndOfUtcDay(dateTo);

        return await context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= from && m.Date <= toInclusive)
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
        DateTime toInclusive = EndOfUtcDay(date);

        return await context.Meals
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Items)
            .Where(m => m.UserId == userId && m.Date >= from && m.Date <= toInclusive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

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

    private async Task<IReadOnlyList<MealProjectionReadModel>> ToMealProjectionReadModelsAsync(
        IReadOnlyCollection<Meal> meals,
        CancellationToken cancellationToken) {
        ImageAssetId[] imageAssetIds = [.. meals
            .SelectMany(static meal => meal.AiSessions)
            .Where(static session => session.ImageAssetId.HasValue)
            .Select(static session => session.ImageAssetId!.Value)
            .Distinct()];

        Dictionary<ImageAssetId, string> imageUrlsById = imageAssetIds.Length == 0
            ? []
            : await context.ImageAssets
                .AsNoTracking()
                .Where(asset => ((IEnumerable<ImageAssetId>)imageAssetIds).Contains(asset.Id))
                .ToDictionaryAsync(asset => asset.Id, asset => asset.Url, cancellationToken)
                .ConfigureAwait(false);

        RecipeId[] legacyRecipeIds = [.. meals
            .SelectMany(static meal => meal.Items)
            .Where(static item => item.RecipeId.HasValue && !item.HasNutritionSnapshot)
            .Select(static item => item.RecipeId!.Value)
            .Distinct()];

        Dictionary<RecipeId, Recipe> legacyRecipesById = legacyRecipeIds.Length == 0
            ? []
            : await context.Recipes
                .AsNoTracking()
                .Where(recipe => ((IEnumerable<RecipeId>)legacyRecipeIds).Contains(recipe.Id))
                .ToDictionaryAsync(recipe => recipe.Id, cancellationToken)
                .ConfigureAwait(false);

        // Product type is not snapshotted, so even complete snapshots need current metadata.
        // One bounded lookup also supplies fallback fields for legacy rows.
        ProductId[] legacyProductIds = [.. meals
            .SelectMany(static meal => meal.Items)
            .Where(static item => item.ProductId.HasValue)
            .Select(static item => item.ProductId!.Value)
            .Distinct()];

        Dictionary<ProductId, Product> legacyProductsById = legacyProductIds.Length == 0
            ? []
            : await context.Products.AsNoTracking()
                .Where(product => ((IEnumerable<ProductId>)legacyProductIds).Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, cancellationToken).ConfigureAwait(false);

        return [.. meals.Select(meal => ToMealProjectionReadModel(meal, imageUrlsById, legacyRecipesById, legacyProductsById))];
    }

    private static MealProjectionReadModel ToMealProjectionReadModel(
        Meal meal,
        IReadOnlyDictionary<ImageAssetId, string> imageUrlsById,
        IReadOnlyDictionary<RecipeId, Recipe> legacyRecipesById,
        IReadOnlyDictionary<ProductId, Product> legacyProductsById) {
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
            ToMealItemProjectionReadModels(meal, legacyRecipesById, legacyProductsById),
            ToMealAiSessionProjectionReadModels(meal, imageUrlsById));
    }

    private static List<MealItemProjectionReadModel> ToMealItemProjectionReadModels(
        Meal meal,
        IReadOnlyDictionary<RecipeId, Recipe> legacyRecipesById,
        IReadOnlyDictionary<ProductId, Product> legacyProductsById) {
        return [.. meal.Items
            .OrderBy(static item => item.Id.Value)
            .Select(item => ToMealItemProjectionReadModel(item, legacyRecipesById, legacyProductsById))];
    }

    private static MealItemProjectionReadModel ToMealItemProjectionReadModel(
        MealItem item,
        IReadOnlyDictionary<RecipeId, Recipe> legacyRecipesById,
        IReadOnlyDictionary<ProductId, Product> legacyProductsById) {
        Recipe? legacyRecipe = item.RecipeId is { } recipeId
            && legacyRecipesById.TryGetValue(recipeId, out Recipe? recipe)
                ? recipe
                : null;
        Product? legacyProduct = item.ProductId is { } productId
            && legacyProductsById.TryGetValue(productId, out Product? product) ? product : null;

        return new MealItemProjectionReadModel(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.SnapshotName ?? legacyProduct?.Name,
            item.SnapshotImageUrl ?? legacyProduct?.ImageUrl,
            item.SnapshotUnit ?? legacyProduct?.BaseUnit.ToString(),
            item.SnapshotBaseAmount ?? legacyProduct?.BaseAmount,
            item.SnapshotCaloriesPerBase ?? legacyProduct?.CaloriesPerBase,
            item.SnapshotProteinsPerBase ?? legacyProduct?.ProteinsPerBase,
            item.SnapshotFatsPerBase ?? legacyProduct?.FatsPerBase,
            item.SnapshotCarbsPerBase ?? legacyProduct?.CarbsPerBase,
            item.SnapshotFiberPerBase ?? legacyProduct?.FiberPerBase,
            item.SnapshotAlcoholPerBase ?? legacyProduct?.AlcoholPerBase,
            legacyProduct?.ProductType,
            item.RecipeId?.Value,
            item.SnapshotName ?? legacyRecipe?.Name,
            item.SnapshotImageUrl ?? legacyRecipe?.ImageUrl,
            GetRecipeServings(item, legacyRecipe),
            item.SnapshotCaloriesPerBase ?? legacyRecipe?.TotalCalories,
            item.SnapshotProteinsPerBase ?? legacyRecipe?.TotalProteins,
            item.SnapshotFatsPerBase ?? legacyRecipe?.TotalFats,
            item.SnapshotCarbsPerBase ?? legacyRecipe?.TotalCarbs,
            item.SnapshotFiberPerBase ?? legacyRecipe?.TotalFiber,
            item.SnapshotAlcoholPerBase ?? legacyRecipe?.TotalAlcohol,
            item.SourceAiItemId?.Value,
            item.Origin);
    }

    private static int? GetRecipeServings(MealItem item, Recipe? legacyRecipe) {
        if (item.HasNutritionSnapshot) {
            return 1;
        }

        return legacyRecipe?.Servings;
    }

    private static List<MealAiSessionProjectionReadModel> ToMealAiSessionProjectionReadModels(
        Meal meal,
        IReadOnlyDictionary<ImageAssetId, string> imageUrlsById) {
        return [.. meal.AiSessions
            .OrderBy(static session => session.RecognizedAtUtc)
            .Select(session => ToMealAiSessionProjectionReadModel(session, imageUrlsById))];
    }

    private static MealAiSessionProjectionReadModel ToMealAiSessionProjectionReadModel(
        MealAiSession session,
        IReadOnlyDictionary<ImageAssetId, string> imageUrlsById) {
        string? imageUrl = session.ImageAssetId.HasValue && imageUrlsById.TryGetValue(session.ImageAssetId.Value, out string? url)
            ? url
            : null;
        return new MealAiSessionProjectionReadModel(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            imageUrl,
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
