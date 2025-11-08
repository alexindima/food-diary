using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly FoodDiaryDbContext _context;

    public ProductRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<Product> AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(UserId userId) =>
        await _context.Products
            .Include(p => p.MealItems)
            .Include(p => p.RecipeIngredients)
            .Where(p => p.UserId == userId || p.Visibility == Visibility.PUBLIC)
            .OrderByDescending(p => p.CreatedOnUtc)
            .ToListAsync();

    public async Task<Product?> GetByIdAsync(ProductId id, UserId userId) =>
        await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || p.Visibility == Visibility.PUBLIC));

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}
