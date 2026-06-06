using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.ExploreRecipes;

public class ExploreRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IQueryHandler<ExploreRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(
        ExploreRecipesQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);

        (IReadOnlyList<(Domain.Entities.Recipes.Recipe Recipe, int UsageCount)>? items, int totalItems) = await recipeRepository.GetExplorePagedAsync(
            pageNumber, pageSize, query.Search, query.Category,
            query.MaxPrepTime, query.SortBy, cancellationToken).ConfigureAwait(false);

        UserId currentUserId = query.UserId.HasValue ? new UserId(query.UserId.Value) : UserId.Empty;

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeModel>(
            items.Select(item => item.Recipe.ToModel(item.UsageCount, item.Recipe.UserId == currentUserId)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
