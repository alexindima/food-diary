using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(GetRecipesQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId.Value);

        var (items, totalItems) = await recipeRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            cancellationToken);

        var recipes = items.Select(item => new {
            item.Recipe,
            item.UsageCount,
            IsOwner = item.Recipe.UserId == userId
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeModel>(
            recipes.Select(r => r.Recipe.ToModel(r.UsageCount, r.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
