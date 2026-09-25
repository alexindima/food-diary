using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        UserId userId, CancellationToken cancellationToken = default) =>
        await Project(userId, BuildFavoritesQuery(userId, search: null).AsNoTracking().OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id)
            .Take(PaginationPolicy.MaxCollectionSize)).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<FavoriteProductReadModel> Items, int Total)> GetPageReadModelsAsync(
        UserId userId, int page, int limit, string? search, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteProduct> query = BuildFavoritesQuery(userId, search);
        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        if (limit == 0) { return ([], total); }
        List<FavoriteProductReadModel> items = await Project(userId, query.AsNoTracking().OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id)
            .Skip((page - 1) * limit).Take(limit)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<IReadOnlyList<FavoriteProductReadModel>> GetByProductIdsReadModelsAsync(UserId userId,
        IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) {
        if (productIds.Count == 0) { return []; }
        ProductId[] ids = [.. productIds];
        return await Project(userId, BuildFavoritesQuery(userId, search: null).AsNoTracking().Where(row => Enumerable.Contains(ids, row.ProductId)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<FavoriteProduct> BuildFavoritesQuery(UserId userId, string? search) {
        var query = context.FavoriteProducts.AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Join(context.Products.AsNoTracking().Where(source => source.UserId == userId || source.Visibility == Visibility.Public),
                favorite => favorite.ProductId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source });
        if (!string.IsNullOrWhiteSpace(search)) {
            string term = "%" + search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            query = query.Where(row => (row.Favorite.Name != null && EF.Functions.ILike(row.Favorite.Name, term, "\\")) ||
                EF.Functions.ILike(row.Source.Name, term, "\\") ||
                (row.Source.Brand != null && EF.Functions.ILike(row.Source.Brand, term, "\\")) ||
                (row.Source.Barcode != null && EF.Functions.ILike(row.Source.Barcode, term, "\\")));

        }
        return query.AsNoTracking().Select(row => row.Favorite);
    }

    private IQueryable<FavoriteProductReadModel> Project(UserId userId, IQueryable<FavoriteProduct> favorites) =>
        favorites.AsNoTracking()
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
                row.Source.UserId.Value) { ImageUrls = row.Source.Images.OrderBy(image => image.Position).Select(image => image.ImageUrl).ToList() });
}
