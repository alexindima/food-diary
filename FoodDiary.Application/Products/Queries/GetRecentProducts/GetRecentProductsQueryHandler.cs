using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryHandler(IRecentProductReadService recentProductReadService)
    : IQueryHandler<GetRecentProductsQuery, Result<IReadOnlyList<ProductModel>>> {
    public async Task<Result<IReadOnlyList<ProductModel>>> Handle(
        GetRecentProductsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<ProductModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<ProductModel> response = await recentProductReadService.GetRecentAsync(
            userId,
            recentLimit,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
}
