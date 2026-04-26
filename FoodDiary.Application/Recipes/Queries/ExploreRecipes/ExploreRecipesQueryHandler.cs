using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
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
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);

        var (items, totalItems) = await recipeRepository.GetExplorePagedAsync(
            pageNumber, pageSize, query.Search, query.Category,
            query.MaxPrepTime, query.SortBy, cancellationToken);

        var currentUserId = query.UserId.HasValue ? new UserId(query.UserId.Value) : UserId.Empty;

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeModel>(
            items.Select(item => item.Recipe.ToModel(item.UsageCount, item.Recipe.UserId == currentUserId)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
