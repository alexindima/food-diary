using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;

public sealed class GetPublicRecipesQueryHandler(IRecipeOverviewReadService recipes)
    : IQueryHandler<GetPublicRecipesQuery, Result<PagedResponse<PublicRecipeModel>>> {
    public async Task<Result<PagedResponse<PublicRecipeModel>>> Handle(GetPublicRecipesQuery query, CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 20, maxPageSize: 50);
        (IReadOnlyList<RecipeOverviewReadItem> items, int total) = await recipes.GetPagedAsync(UserId.Empty, includePublic: true, page, limit,
            new RecipeQueryFilters(query.Search, query.Category, query.MaxTotalTime), cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedResponse<PublicRecipeModel>(items.Select(item => item.ToPublicModel()).ToList(),
            page, limit, (int)Math.Ceiling(total / (double)limit), total));
    }
}
