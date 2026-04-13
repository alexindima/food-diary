using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryHandler(
    IProductRepository productRepository,
    IRecentItemRepository recentItemRepository,
    IFavoriteProductRepository favoriteProductRepository)
    : IQueryHandler<GetProductsOverviewQuery, Result<ProductOverviewModel>> {
    public async Task<Result<ProductOverviewModel>> Handle(
        GetProductsOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var recentLimit = Math.Clamp(query.RecentLimit, 1, 50);
        var favoriteLimit = Math.Clamp(query.FavoriteLimit, 1, 50);
        var productTypes = query.ProductTypes?
            .Select(type => Enum.TryParse<ProductType>(type, true, out var parsed) ? parsed : (ProductType?)null)
            .OfType<ProductType>()
            .Distinct()
            .ToArray();
        var selectedProductTypes = productTypes is { Length: > 0 } ? productTypes.ToHashSet() : null;

        var (items, totalItems) = await productRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            productTypes is { Length: > 0 } ? productTypes : null,
            cancellationToken);

        var allProducts = items.Select(item => new {
            item.Product,
            item.UsageCount,
            IsOwner = item.Product.UserId == userId
        }).ToList();
        var allFavorites = await favoriteProductRepository.GetAllAsync(userId, cancellationToken);
        var favoriteItems = allFavorites
            .Take(favoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.ProductId);

        var recentItems = Array.Empty<(Domain.Entities.Products.Product Product, int UsageCount, bool IsOwner)>();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var recentResponses = Array.Empty<ProductModel>();
        if (string.IsNullOrWhiteSpace(query.Search)) {
            var recents = await recentItemRepository.GetRecentProductsAsync(userId, recentLimit, cancellationToken);
            if (recents.Count > 0) {
                var recentIds = recents.Select(x => x.ProductId).ToList();
                var productsById = await productRepository.GetByIdsWithUsageAsync(
                    recentIds,
                    userId,
                    query.IncludePublic,
                    cancellationToken);

                recentItems = recentIds
                    .Where(productsById.ContainsKey)
                    .Select(id => {
                        var item = productsById[id];
                        if (selectedProductTypes is not null && !selectedProductTypes.Contains(item.Product.ProductType)) {
                            return ((Domain.Entities.Products.Product Product, int UsageCount, bool IsOwner)?)null;
                        }

                        return (item.Product, item.UsageCount, item.Product.UserId == userId);
                    })
                    .Where(item => item is not null)
                    .Select(item => item!.Value)
                    .ToArray();
            }
        }

        var favoriteProductIds = allProducts
            .Select(x => x.Product.Id)
            .Concat(recentItems.Select(x => x.Product.Id))
            .Distinct()
            .ToArray();
        var favoritesByProductId = favoriteLookup
            .Where(pair => favoriteProductIds.Contains(pair.Key))
            .ToDictionary();

        var allPaged = new PagedResponse<ProductModel>(
            allProducts.Select(x => {
                var favorite = favoritesByProductId.GetValueOrDefault(x.Product.Id);
                return x.Product.ToModel(
                    x.UsageCount,
                    x.IsOwner,
                    favorite is not null,
                    favorite?.Id.Value);
            }).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        recentResponses = recentItems
            .Select(x => {
                var favorite = favoritesByProductId.GetValueOrDefault(x.Product.Id);
                return x.Product.ToModel(
                    x.UsageCount,
                    x.IsOwner,
                    favorite is not null,
                    favorite?.Id.Value);
            })
            .ToArray();

        return Result.Success(new ProductOverviewModel(recentResponses, allPaged, favoriteItems, allFavorites.Count));
    }
}
