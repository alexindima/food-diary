using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    Task<Product> AddAsync(Product product);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(ProductId id, UserId userId);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}
