using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence.Products;

public sealed class ProductRepository(ProductsDbContext context, IProductUsageQuery usageQuery, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IProductRepository {
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        await context.Products.AddAsync(product, cancellationToken);
        return product;
    }

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        return await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == id && (includePublic
                    ? p.UserId == userId || p.Visibility == Visibility.Public
                    : p.UserId == userId),
                cancellationToken);
    }

    public async Task<Product?> GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        if (context.Database.CurrentTransaction is null || !context.Database.IsRelational()) {
            return await context.Products
                .AsTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == id && (includePublic
                        ? p.UserId == userId || p.Visibility == Visibility.Public
                        : p.UserId == userId),
                    cancellationToken);
        }

        IQueryable<Product> lockedProducts = includePublic
            ? context.Products.FromSqlInterpolated(
                $"""SELECT *, xmin FROM "Products" WHERE "Id" = {id.Value} AND ("UserId" = {userId.Value} OR "Visibility" = {(int)Visibility.Public}) FOR UPDATE""")
            : context.Products.FromSqlInterpolated(
                $"""SELECT *, xmin FROM "Products" WHERE "Id" = {id.Value} AND "UserId" = {userId.Value} FOR UPDATE""");
        return await lockedProducts.AsSplitQuery().SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, Product>();
        }

        IQueryable<Product> query = context.Products.AsNoTracking();
        query = query.Where(p => productIds.Contains(p.Id) && (includePublic
            ? p.UserId == userId || p.Visibility == Visibility.Public
            : p.UserId == userId));

        List<Product> products = await query.ToListAsync(cancellationToken);
        return products.ToDictionary(p => p.Id);
    }

    public Task<int> GetUsageCountAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        usageQuery.GetUsageCountAsync(id, userId, includePublic, cancellationToken);

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken);
        }
        Product? tracked = await context.Products.FindAsync([product.Id], cancellationToken);
        if (tracked is not null) {
            context.Products.Remove(tracked);
        }
    }

}
