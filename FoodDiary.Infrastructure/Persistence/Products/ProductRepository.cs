using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Products;

public class ProductRepository(FoodDiaryDbContext context) : IProductRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        await context.Products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return product;
    }

    public async Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Product> query = context.Products
            .AsNoTracking()
            .Where(includePublic
                ? p => p.UserId == userId || p.Visibility == Visibility.Public
                : p => p.UserId == userId);

        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            string normalizedSearch = $"%{EscapeLikePattern(filters.Search.Trim())}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Brand ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Category ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Barcode ?? string.Empty, normalizedSearch, LikeEscapeCharacter));
        }

        if (filters.ProductTypes is { Count: > 0 }) {
            query = query.Where(p => filters.ProductTypes.Contains(p.ProductType));
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.Where(p => p.CaloriesPerBase >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.Where(p => p.CaloriesPerBase <= filters.CaloriesTo.Value);
        }

        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.Where(p => p.ImageUrl != null || p.ImageAssetId != null)
                : query.Where(p => p.ImageUrl == null && p.ImageAssetId == null);
        }

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        IOrderedQueryable<Product> orderedQuery = query.OrderByDescending(p => p.CreatedOnUtc);
        int skip = (pageNumber - 1) * pageSize;
        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new {
                Product = p,
                UsageCount = p.MealItems.Count + p.RecipeIngredients.Count,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items
            .ConvertAll(x => (x.Product, x.UsageCount))
, totalItems);
    }

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.MealItems)
            .Include(p => p.RecipeIngredients)
            .FirstOrDefaultAsync(
                p => p.Id == id && (includePublic
                    ? p.UserId == userId || p.Visibility == Visibility.Public
                    : p.UserId == userId),
                cancellationToken).ConfigureAwait(false);

    public async Task<Product?> GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .AsTracking()
            .AsSplitQuery()
            .Include(p => p.MealItems)
            .Include(p => p.RecipeIngredients)
            .FirstOrDefaultAsync(
                p => p.Id == id && (includePublic
                    ? p.UserId == userId || p.Visibility == Visibility.Public
                    : p.UserId == userId),
                cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, Product>();
        }

        IQueryable<Product> query = context.Products.AsNoTracking();
        query = query.Where(p => productIds.Contains(p.Id) && (includePublic
            ? p.UserId == userId || p.Visibility == Visibility.Public
            : p.UserId == userId));

        List<Product> products = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return products.ToDictionary(p => p.Id);
    }

    public async Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, (Product Product, int UsageCount)>();
        }

        var items = await context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && (includePublic
                ? p.UserId == userId || p.Visibility == Visibility.Public
                : p.UserId == userId))
            .Select(p => new {
                Product = p,
                UsageCount = p.MealItems.Count + p.RecipeIngredients.Count,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return items.ToDictionary(x => x.Product.Id, x => (x.Product, x.UsageCount));
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        context.Products.Update(product);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        Product? tracked = await context.Products.FindAsync([product.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.Products.Remove(tracked);
        }
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
