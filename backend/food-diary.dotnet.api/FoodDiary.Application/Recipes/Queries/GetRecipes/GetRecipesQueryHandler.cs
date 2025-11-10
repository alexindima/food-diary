using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Recipes;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IQueryHandler<GetRecipesQuery, Result<PagedResponse<RecipeResponse>>>
{
    public async Task<Result<PagedResponse<RecipeResponse>>> Handle(GetRecipesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var userId = query.UserId!.Value;

        var (items, totalItems) = await recipeRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            cancellationToken);

        var recipes = items.Select(item => new
        {
            item.Recipe,
            item.UsageCount,
            IsOwner = item.Recipe.UserId == userId
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeResponse>(
            recipes.Select(r => r.Recipe.ToResponse(r.UsageCount, r.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
