using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteProducts;

public sealed class FavoriteProductRepository(DbSet<FavoriteProduct> favorites, IFavoriteProductQuery queries) : IFavoriteProductRepository {
    public Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        favorites.Add(favorite);
        return Task.FromResult(favorite);
    }

    public Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        favorites.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<FavoriteProduct?> GetByIdAsync(FavoriteProductId id, UserId userId,
        bool asTracking = false, CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProductId> ids = await queries.GetAccessibleIdsAsync(userId, favoriteId: id, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (ids.Count == 0) { return null; }
        return await GetOwnedByIdAsync(id, userId, asTracking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteProduct?> GetOwnedByIdAsync(FavoriteProductId id, UserId userId,
        bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteProduct> query = asTracking ? favorites.AsTracking() : favorites.AsNoTracking();
        return await query.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteProduct?> GetByProductIdAsync(ProductId productId, UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProductId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: [productId], cancellationToken: cancellationToken).ConfigureAwait(false);
        return await favorites.AsNoTracking().FirstOrDefaultAsync(f => f.UserId == userId && ids.Contains(f.Id), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByProductIdAsync(ProductId productId, UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProductId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: [productId], cancellationToken: cancellationToken).ConfigureAwait(false);
        return ids.Count != 0;
    }

    public async Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteProductId> ids = await queries.GetAccessibleIdsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await favorites.AsNoTracking().Where(f => f.UserId == userId && ids.Contains(f.Id))
            .OrderByDescending(f => f.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
        queries.GetAllReadModelsAsync(userId, cancellationToken);

    public async Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(UserId userId,
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) {
        if (productIds.Count == 0) { return new Dictionary<ProductId, FavoriteProduct>(); }
        IReadOnlyList<FavoriteProductId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: productIds, cancellationToken: cancellationToken).ConfigureAwait(false);
        List<FavoriteProduct> results = await favorites.AsNoTracking()
            .Where(f => f.UserId == userId && ids.Contains(f.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.ToDictionary(f => f.ProductId);
    }
}
