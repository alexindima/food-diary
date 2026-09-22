using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Mappings;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMealPage;

public sealed class GetFavoriteMealPageQueryHandler(IFavoriteMealQuery queries, ICurrentUserAccessService access)
    : IQueryHandler<GetFavoriteMealPageQuery, Result<PagedResponse<FavoriteMealModel>>> {
    public async Task<Result<PagedResponse<FavoriteMealModel>>> Handle(GetFavoriteMealPageQuery request, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(request.UserId, access, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<FavoriteMealModel>>(user);
        }
        (IReadOnlyList<FavoriteMealReadModel> items, int total) = await queries.GetPageReadModelsAsync(user.Value, request.Page, request.Limit, request.Search?.Trim(), cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedResponse<FavoriteMealModel>(items.Select(item => item.ToModel()).ToArray(), request.Page, request.Limit,
            (int)Math.Ceiling((double)total / request.Limit), total));
    }
}
