using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public async Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Where(includePublic
                ? p => p.UserId == userId || p.Visibility == Visibility.PUBLIC
                : p => p.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(normalizedSearch) ||
                (p.Brand != null && p.Brand.ToLower().Contains(normalizedSearch)) ||
                (p.Category != null && p.Category.ToLower().Contains(normalizedSearch)) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(normalizedSearch)));
        }

        var orderedQuery = query.OrderByDescending(p => p.CreatedOnUtc);
        var totalItems = await orderedQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;
        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new
            {
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
        await _context.Products
            .AsNoTracking()
            .Include(p => p.MealItems)
            .Include(p => p.RecipeIngredients)
            .FirstOrDefaultAsync(
                p => p.Id == id && (includePublic
                    ? p.UserId == userId || p.Visibility == Visibility.PUBLIC
                    : p.UserId == userId),
                cancellationToken);

    public async Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default)
    {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, Product>();
        }

        IQueryable<Product> query = _context.Products.AsNoTracking();
        query = query.Where(p => productIds.Contains(p.Id) && (includePublic
            ? p.UserId == userId || p.Visibility == Visibility.PUBLIC
            : p.UserId == userId));

        var products = await query.ToListAsync(cancellationToken);
        return products.ToDictionary(p => p.Id);
    }

    public async Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default)
    {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, (Product Product, int UsageCount)>();
        }

        var items = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && (includePublic
                ? p.UserId == userId || p.Visibility == Visibility.PUBLIC
                : p.UserId == userId))
            .Select(p => new
            {
                Product = p,
                UsageCount = p.MealItems.Count + p.RecipeIngredients.Count
            })
            .ToListAsync(cancellationToken);

        return items.ToDictionary(x => x.Product.Id, x => (x.Product, x.UsageCount));
    }

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
