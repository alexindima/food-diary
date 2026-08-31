using Microsoft.Extensions.Caching.Memory;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Products;

public sealed class CachedProductRepository(
    ProductRepository inner,
    IMemoryCache cache) : IProductRepository {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        Product result = await inner.AddAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(result.Id);
        return result;
    }

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        string key = CacheKey(id);
        if (cache.TryGetValue(key, out Product? cached)) {
            return IsAccessible(cached, userId, includePublic) ? cached : null;
        }

        Product? product = await inner.GetByIdAsync(id, userId, includePublic, cancellationToken).ConfigureAwait(false);
        if (product is not null) {
            cache.Set(key, product, CacheDuration);
        }

        return product;
    }

    public Task<Product?> GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdForUpdateAsync(id, userId, includePublic, cancellationToken);

    public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdsAsync(ids, userId, includePublic, cancellationToken);

    public Task<int> GetUsageCountAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        inner.GetUsageCountAsync(id, userId, includePublic, cancellationToken);

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        await inner.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(product.Id);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        await inner.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(product.Id);
    }

    private void Evict(ProductId id) {
        cache.Remove(CacheKey(id));
    }

    private static bool IsAccessible(Product? product, UserId userId, bool includePublic) =>
        product is not null && (product.UserId == userId || (includePublic && product.Visibility == Visibility.Public));

    private static string CacheKey(ProductId id) => $"Product:{id}";
}
