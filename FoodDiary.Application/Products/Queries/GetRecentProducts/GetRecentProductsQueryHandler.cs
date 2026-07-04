using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryHandler(
    IRecentItemReadRepository recentItemRepository,
    IProductReadRepository productRepository)
    : IQueryHandler<GetRecentProductsQuery, Result<IReadOnlyList<ProductModel>>> {
    public async Task<Result<IReadOnlyList<ProductModel>>> Handle(
        GetRecentProductsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ProductModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<RecentProductUsage> recents = await recentItemRepository.GetRecentProductsAsync(userId, recentLimit, cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return Result.Success<IReadOnlyList<ProductModel>>(Array.Empty<ProductModel>());
        }

        var idsInOrder = recents.Select(x => x.ProductId).ToList();
        IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)> productsById = await productRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        var response = idsInOrder
            .Where(productsById.ContainsKey)
            .Select(id => {
                (Product Product, int UsageCount) item = productsById[id];
                return item.Product.ToModel(item.UsageCount, item.Product.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<ProductModel>>(response);
    }
}
