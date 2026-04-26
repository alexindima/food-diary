using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.FavoriteProducts;

public class FavoriteProductRepository(FoodDiaryDbContext context) : IFavoriteProductRepository {
    public async Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        context.FavoriteProducts.Add(favorite);
        await context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        context.FavoriteProducts.Remove(favorite);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FavoriteProduct?> GetByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = context.FavoriteProducts
            .Include(f => f.Product)
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken);
    }

    public async Task<FavoriteProduct?> GetByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.ProductId == productId && f.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .Include(f => f.Product)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default) {
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, FavoriteProduct>();
        }

        var favorites = await context.FavoriteProducts
            .AsNoTracking()
            .Where(f => f.UserId == userId && productIds.Contains(f.ProductId))
            .ToListAsync(cancellationToken);

        return favorites.ToDictionary(f => f.ProductId);
    }
}
