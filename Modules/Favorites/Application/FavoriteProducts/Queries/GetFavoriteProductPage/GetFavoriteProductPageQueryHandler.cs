using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProductPage;

public sealed class GetFavoriteProductPageQueryHandler(IFavoriteProductQuery queries, ICurrentUserAccessService access)
    : IQueryHandler<GetFavoriteProductPageQuery, Result<PagedResponse<FavoriteProductModel>>> {
    public async Task<Result<PagedResponse<FavoriteProductModel>>> Handle(GetFavoriteProductPageQuery request, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(request.UserId, access, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<FavoriteProductModel>>(user);
        }
        (IReadOnlyList<FavoriteProductReadModel> items, int total) = await queries.GetPageReadModelsAsync(user.Value, request.Page, request.Limit, request.Search?.Trim(), cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedResponse<FavoriteProductModel>(items.Select(item => item.ToModel()).ToArray(), request.Page, request.Limit,
            (int)Math.Ceiling((double)total / request.Limit), total));
    }
}
