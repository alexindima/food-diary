using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.FavoriteProducts;

public sealed class FavoriteProductRepository(FoodDiaryDbContext context) : IFavoriteProductRepository {
    public Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        context.FavoriteProducts.Add(favorite);
        return Task.FromResult(favorite);
    }

    public Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
        context.FavoriteProducts.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<FavoriteProduct?> GetByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<FavoriteProduct> query = context.FavoriteProducts
            .Include(f => f.Product)
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteProduct?> GetByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.ProductId == productId && f.UserId == userId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .AnyAsync(
                f => f.ProductId == productId && f.UserId == userId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .Include(f => f.Product)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Select(f => new FavoriteProductReadModel(
                f.Id.Value,
                f.ProductId.Value,
                f.UserId.Value,
                f.Name,
                f.CreatedAtUtc,
                f.Product.Name,
                f.Product.Brand,
                f.Product.Barcode,
                f.Product.UserId == f.UserId ? f.Product.Comment : null,
                f.Product.ImageUrl,
                f.Product.CaloriesPerBase,
                f.Product.ProteinsPerBase,
                f.Product.FatsPerBase,
                f.Product.CarbsPerBase,
                f.Product.FiberPerBase,
                f.Product.AlcoholPerBase,
                f.Product.ProductType,
                f.Product.BaseUnit,
                f.PreferredPortionAmount,
                f.Product.DefaultPortionAmount,
                f.Product.UserId.Value))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default) {
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, FavoriteProduct>();
        }

        List<FavoriteProduct> favorites = await context.FavoriteProducts
            .AsNoTracking()
            .Where(f => f.UserId == userId && productIds.Contains(f.ProductId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(f => f.ProductId);
    }
}
