using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductsWithRecent;

public sealed class GetProductsWithRecentQueryHandler(
    IProductRepository productRepository,
    IRecentItemRepository recentItemRepository)
    : IQueryHandler<GetProductsWithRecentQuery, Result<ProductListWithRecentModel>> {
    public async Task<Result<ProductListWithRecentModel>> Handle(
        GetProductsWithRecentQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductListWithRecentModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var recentLimit = Math.Clamp(query.RecentLimit, 1, 50);
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

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var allPaged = new PagedResponse<ProductModel>(
            allProducts.Select(x => x.Product.ToModel(x.UsageCount, x.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        var recentResponses = Array.Empty<ProductModel>();
        if (string.IsNullOrWhiteSpace(query.Search)) {
            var recents = await recentItemRepository.GetRecentProductsAsync(userId, recentLimit, cancellationToken);
            if (recents.Count <= 0) {
                return Result.Success(new ProductListWithRecentModel(recentResponses, allPaged));
            }

            var recentIds = recents.Select(x => x.ProductId).ToList();
            var productsById = await productRepository.GetByIdsWithUsageAsync(
                recentIds,
                userId,
                query.IncludePublic,
                cancellationToken);

            recentResponses = recentIds
                .Where(productsById.ContainsKey)
                .Select(id => {
                    var item = productsById[id];
                    if (selectedProductTypes is not null && !selectedProductTypes.Contains(item.Product.ProductType)) {
                        return null;
                    }

                    return item.Product.ToModel(item.UsageCount, item.Product.UserId == userId);
                })
                .Where(response => response is not null)
                .Select(response => response!)
                .ToArray();
        }

        return Result.Success(new ProductListWithRecentModel(recentResponses, allPaged));
    }
}
