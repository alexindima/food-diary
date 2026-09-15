using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Favorites;

public sealed class FavoriteProductQuery(ICompositionReadContext context) : IFavoriteProductQuery {
    public async Task<IReadOnlyList<FavoriteProductId>> GetAccessibleIdsAsync(
        UserId userId, FavoriteProductId? favoriteId = null,
        IReadOnlyCollection<ProductId>? sourceIds = null, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteProduct> query = context.FavoriteProducts.AsNoTracking().Where(f => f.UserId == userId &&
            context.Products.AsNoTracking().Any(source => source.Id == f.ProductId &&
                (source.UserId == userId || source.Visibility == Visibility.Public)));
        if (favoriteId.HasValue) { query = query.AsNoTracking().Where(f => f.Id == favoriteId.Value); }
        if (sourceIds is not null) { query = query.AsNoTracking().Where(f => sourceIds.Contains(f.ProductId)); }
        return await query.AsNoTracking().Select(f => f.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
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

}
