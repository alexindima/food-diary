using Microsoft.Extensions.Caching.Memory;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Products;

public sealed class CachedProductRepository(
    ProductRepository inner,
    IMemoryCache cache) : IProductRepository {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        Product result = await inner.AddAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(result.Id, result.UserId);
        return result;
    }

    public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) =>
        inner.GetPagedAsync(userId, includePublic, page, limit, filters, cancellationToken);

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        string key = CacheKey(id, userId, includePublic);
        if (cache.TryGetValue(key, out Product? cached)) {
            return cached;
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

    public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        inner.GetByIdsWithUsageAsync(ids, userId, includePublic, cancellationToken);

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        await inner.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(product.Id, product.UserId);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        await inner.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
        Evict(product.Id, product.UserId);
    }

    private void Evict(ProductId id, UserId userId) {
        cache.Remove(CacheKey(id, userId, includePublic: true));
        cache.Remove(CacheKey(id, userId, includePublic: false));
    }

    private static string CacheKey(ProductId id, UserId userId, bool includePublic) =>
        $"Product:{id}:{userId}:{includePublic}";
}
