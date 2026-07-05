using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.ExploreRecipes;

public sealed class ExploreRecipesQueryHandler(IRecipeOverviewReadService recipeOverviewReadService)
    : IQueryHandler<ExploreRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(
        ExploreRecipesQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);

        UserId currentUserId = query.UserId.HasValue ? new UserId(query.UserId.Value) : UserId.Empty;

        (IReadOnlyList<RecipeOverviewReadItem> items, int totalItems) = await recipeOverviewReadService.GetExplorePagedAsync(
            currentUserId, pageNumber, pageSize, query.Search, query.Category,
            query.MaxPrepTime, query.SortBy, cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeModel>(
            items.Select(recipe => recipe.ToModel()).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
