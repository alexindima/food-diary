using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Products;

public class ProductRepository(FoodDiaryDbContext context) : IProductRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        IReadOnlyCollection<ProductType>? productTypes = null,
        CancellationToken cancellationToken = default) {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        var query = context.Products
            .AsNoTracking()
            .Where(includePublic
                ? p => p.UserId == userId || p.Visibility == Visibility.Public
                : p => p.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search)) {
            var normalizedSearch = $"%{EscapeLikePattern(search.Trim())}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Brand ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Category ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Barcode ?? string.Empty, normalizedSearch, LikeEscapeCharacter));
        }

        if (productTypes is { Count: > 0 }) {
            query = query.Where(p => productTypes.Contains(p.ProductType));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderByDescending(p => p.CreatedOnUtc);
        var skip = (pageNumber - 1) * pageSize;
        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new {
                Product = p,
                UsageCount = p.MealItems.Count + p.RecipeIngredients.Count
            })
            .ToListAsync(cancellationToken);

        return (items
            .Select(x => (x.Product, x.UsageCount))
            .ToList(), totalItems);
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
                cancellationToken);

    public async Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, Product>();
        }

        var query = context.Products.AsNoTracking();
        query = query.Where(p => productIds.Contains(p.Id) && (includePublic
            ? p.UserId == userId || p.Visibility == Visibility.Public
            : p.UserId == userId));

        var products = await query.ToListAsync(cancellationToken);
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
                UsageCount = p.MealItems.Count + p.RecipeIngredients.Count
            })
            .ToListAsync(cancellationToken);

        return items.ToDictionary(x => x.Product.Id, x => (x.Product, x.UsageCount));
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        context.Products.Update(product);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        var tracked = await context.Products.FindAsync([product.Id], cancellationToken);
        if (tracked is not null) {
            context.Products.Remove(tracked);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
