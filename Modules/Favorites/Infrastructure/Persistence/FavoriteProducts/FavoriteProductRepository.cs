using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteProducts;

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
        IQueryable<FavoriteProduct> query = context.FavoriteProducts.Where(
            favorite => favorite.Id == id && favorite.UserId == userId &&
                context.Products.AsNoTracking().Any(source => source.Id == favorite.ProductId &&
                    (source.UserId == userId || source.Visibility == Visibility.Public)));

        // EF tracking applies to the whole query, including nested access checks.
        query = asTracking ? query.AsTracking() : query.AsNoTracking();
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteProduct?> GetOwnedByIdAsync(
        FavoriteProductId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<FavoriteProduct> query = context.FavoriteProducts;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            favorite => favorite.Id == id && favorite.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteProduct?> GetByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.ProductId == productId && f.UserId == userId &&
                    context.Products.AsNoTracking().Any(source => source.Id == f.ProductId && (source.UserId == userId || source.Visibility == Visibility.Public)),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByProductIdAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .AnyAsync(
                f => f.ProductId == productId && f.UserId == userId &&
                    context.Products.AsNoTracking().Any(source => source.Id == f.ProductId && (source.UserId == userId || source.Visibility == Visibility.Public)),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .Where(f => f.UserId == userId &&
                context.Products.AsNoTracking().Any(source => source.Id == f.ProductId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteProducts
            .AsNoTracking()
            .Where(f => f.UserId == userId &&
                context.Products.AsNoTracking().Any(source => source.Id == f.ProductId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(PaginationPolicy.MaxCollectionSize)
            .Join(context.Products.AsNoTracking(), favorite => favorite.ProductId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source })
            .Select(row => new FavoriteProductReadModel(
                row.Favorite.Id.Value,
                row.Favorite.ProductId.Value,
                row.Favorite.UserId.Value,
                row.Favorite.Name,
                row.Favorite.CreatedAtUtc,
                row.Source.Name,
                row.Source.Brand,
                row.Source.Barcode,
                row.Source.UserId == row.Favorite.UserId ? row.Source.Comment : null,
                row.Source.ImageUrl,
                row.Source.CaloriesPerBase,
                row.Source.ProteinsPerBase,
                row.Source.FatsPerBase,
                row.Source.CarbsPerBase,
                row.Source.FiberPerBase,
                row.Source.AlcoholPerBase,
                row.Source.ProductType,
                row.Source.BaseUnit,
                row.Favorite.PreferredPortionAmount,
                row.Source.DefaultPortionAmount,
                row.Source.UserId.Value))
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
            .Where(f => f.UserId == userId && productIds.Contains(f.ProductId) &&
                context.Products.AsNoTracking().Any(source => source.Id == f.ProductId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(f => f.ProductId);
    }
}
