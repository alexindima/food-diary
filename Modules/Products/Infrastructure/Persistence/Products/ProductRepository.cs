using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Products;

public sealed class ProductRepository(ProductsDbContext context, IProductUsageQuery usageQuery, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IProductRepository {
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        await context.Products.AddAsync(product, cancellationToken).ConfigureAwait(false);
        return product;
    }

    public async Task<Product?> GetByIdAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == id && (includePublic
                    ? p.UserId == userId || p.Visibility == Visibility.Public
                    : p.UserId == userId),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<Product?> GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (context.Database.CurrentTransaction is null || !context.Database.IsRelational()) {
            return await context.Products
                .AsTracking()
                .FirstOrDefaultAsync(
                    p => p.Id == id && (includePublic
                        ? p.UserId == userId || p.Visibility == Visibility.Public
                        : p.UserId == userId),
                    cancellationToken).ConfigureAwait(false);
        }

        IQueryable<Product> lockedProducts = includePublic
            ? context.Products.FromSqlInterpolated(
                $"""SELECT *, xmin FROM "Products" WHERE "Id" = {id.Value} AND ("UserId" = {userId.Value} OR "Visibility" = {(int)Visibility.Public}) FOR UPDATE""")
            : context.Products.FromSqlInterpolated(
                $"""SELECT *, xmin FROM "Products" WHERE "Id" = {id.Value} AND "UserId" = {userId.Value} FOR UPDATE""");
        return await lockedProducts.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
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

    public Task<int> GetUsageCountAsync(
        ProductId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) =>
        usageQuery.GetUsageCountAsync(id, userId, includePublic, cancellationToken);

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        context.Products.Update(product);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        Product? tracked = await context.Products.FindAsync([product.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.Products.Remove(tracked);
        }
    }

}
