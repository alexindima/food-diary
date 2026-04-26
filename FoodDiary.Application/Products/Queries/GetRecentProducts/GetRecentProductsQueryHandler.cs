using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryHandler(
    IRecentItemRepository recentItemRepository,
    IProductRepository productRepository)
    : IQueryHandler<GetRecentProductsQuery, Result<IReadOnlyList<ProductModel>>> {
    public async Task<Result<IReadOnlyList<ProductModel>>> Handle(
        GetRecentProductsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ProductModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var recentLimit = Math.Clamp(query.Limit, 1, 50);

        var recents = await recentItemRepository.GetRecentProductsAsync(userId, recentLimit, cancellationToken);
        if (recents.Count == 0) {
            return Result.Success<IReadOnlyList<ProductModel>>(Array.Empty<ProductModel>());
        }

        var idsInOrder = recents.Select(x => x.ProductId).ToList();
        var productsById = await productRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken);

        var response = idsInOrder
            .Where(productsById.ContainsKey)
            .Select(id => {
                var item = productsById[id];
                return item.Product.ToModel(item.UsageCount, item.Product.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<ProductModel>>(response);
    }
}
