using Microsoft.Extensions.Caching.Memory;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Products;

public class CachedProductRepository(
    ProductRepository inner,
    IMemoryCache cache) : IProductRepository {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        var result = await inner.AddAsync(product, cancellationToken);
        Evict(result.Id, result.UserId);
        return result;
    }

    public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        IReadOnlyCollection<ProductType>? productTypes = null,
        CancellationToken cancellationToken = default) =>
        inner.GetPagedAsync(userId, includePublic, page, limit, search, productTypes, cancellationToken);

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var key = CacheKey(id, userId, includePublic);
        if (cache.TryGetValue(key, out Product? cached))
            return cached;

        var product = await inner.GetByIdAsync(id, userId, includePublic, cancellationToken);
        if (product is not null)
            cache.Set(key, product, CacheDuration);

        return product;
    }

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
        await inner.UpdateAsync(product, cancellationToken);
        Evict(product.Id, product.UserId);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        await inner.DeleteAsync(product, cancellationToken);
        Evict(product.Id, product.UserId);
    }

    private void Evict(ProductId id, UserId userId) {
        cache.Remove(CacheKey(id, userId, includePublic: true));
        cache.Remove(CacheKey(id, userId, includePublic: false));
    }

    private static string CacheKey(ProductId id, UserId userId, bool includePublic) =>
        $"Product:{id}:{userId}:{includePublic}";
}
