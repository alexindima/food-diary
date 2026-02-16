using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryHandler(
    IRecentItemRepository recentItemRepository,
    IProductRepository productRepository)
    : IQueryHandler<GetRecentProductsQuery, Result<IReadOnlyList<ProductResponse>>>
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> Handle(
        GetRecentProductsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.UserId is null || query.UserId == Domain.ValueObjects.UserId.Empty)
        {
            return Result.Failure<IReadOnlyList<ProductResponse>>(Errors.Authentication.InvalidToken);
        }

        var userId = query.UserId.Value;
        var recentLimit = Math.Clamp(query.Limit, 1, 50);

        var recents = await recentItemRepository.GetRecentProductsAsync(userId, recentLimit, cancellationToken);
        if (recents.Count == 0)
        {
            return Result.Success<IReadOnlyList<ProductResponse>>(Array.Empty<ProductResponse>());
        }

        var idsInOrder = recents.Select(x => x.ProductId).ToList();
        var productsById = await productRepository.GetByIdsWithUsageAsync(
            idsInOrder,
            userId,
            query.IncludePublic,
            cancellationToken);

        var response = idsInOrder
            .Where(productsById.ContainsKey)
            .Select(id =>
            {
                var item = productsById[id];
                return item.Product.ToResponse(item.UsageCount, item.Product.UserId == userId);
            })
            .ToList();

        return Result.Success<IReadOnlyList<ProductResponse>>(response);
    }
}
