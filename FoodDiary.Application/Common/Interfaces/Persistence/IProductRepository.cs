using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IProductRepository {
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        IReadOnlyCollection<ProductType>? productTypes = null,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
}
