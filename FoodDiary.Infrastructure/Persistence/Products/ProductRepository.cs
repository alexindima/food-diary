using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Products;

public sealed class ProductRepository(FoodDiaryDbContext context) : IProductRepository {
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        await context.Products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return product;
    }

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .AsNoTracking()
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

    public async Task<int> GetUsageCountAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id && (includePublic
                ? p.UserId == userId || p.Visibility == Visibility.Public
                : p.UserId == userId))
            .Select(p => p.MealItems.Count + p.RecipeIngredients.Count)
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

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

}
