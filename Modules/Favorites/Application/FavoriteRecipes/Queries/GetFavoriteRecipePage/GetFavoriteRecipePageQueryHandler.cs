using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Mappings;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipePage;

public sealed class GetFavoriteRecipePageQueryHandler(IFavoriteRecipeQuery queries, ICurrentUserAccessService access)
    : IQueryHandler<GetFavoriteRecipePageQuery, Result<PagedResponse<FavoriteRecipeModel>>> {
    public async Task<Result<PagedResponse<FavoriteRecipeModel>>> Handle(GetFavoriteRecipePageQuery request, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(request.UserId, access, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<FavoriteRecipeModel>>(user);
        }
        (IReadOnlyList<FavoriteRecipeReadModel> items, int total) = await queries.GetPageReadModelsAsync(user.Value, request.Page, request.Limit, request.Search?.Trim(), cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedResponse<FavoriteRecipeModel>(items.Select(item => item.ToModel()).ToArray(), request.Page, request.Limit,
            (int)Math.Ceiling((double)total / request.Limit), total));
    }
}
