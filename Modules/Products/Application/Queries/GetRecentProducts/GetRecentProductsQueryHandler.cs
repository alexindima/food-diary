using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Products.Application.Queries.GetRecentProducts;

public sealed class GetRecentProductsQueryHandler(
    RecentProductLoader recentProductLoader,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecentProductsQuery, Result<IReadOnlyList<ProductModel>>> {
    public async Task<Result<IReadOnlyList<ProductModel>>> Handle(
        GetRecentProductsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ProductModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int recentLimit = Math.Clamp(query.Limit, 1, 50);

        IReadOnlyList<ProductModel> response = await GetRecentAsync(
            userId,
            recentLimit,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }
    private async Task<IReadOnlyList<ProductModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<ProductOverviewReadItem> items = await recentProductLoader.LoadAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Select(item => item.ToModel())];
    }
}
