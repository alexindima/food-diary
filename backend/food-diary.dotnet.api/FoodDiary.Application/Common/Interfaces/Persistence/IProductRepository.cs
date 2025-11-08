using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    Task<Product> AddAsync(Product product);
    Task<IEnumerable<Product>> GetAllAsync(UserId userId);
    Task<Product?> GetByIdAsync(ProductId id, UserId userId);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}
