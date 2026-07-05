using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public sealed class GetRecipesQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(GetRecipesQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<RecipeModel>>(accessError);
        }

        (IReadOnlyList<RecipeOverviewReadItem> items, int totalItems) = await recipeOverviewReadService.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            new RecipeQueryFilters(
                query.Search,
                query.Category,
                query.MaxTotalTime,
                query.CaloriesFrom,
                query.CaloriesTo,
                query.HasImage),
            cancellationToken).ConfigureAwait(false);

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
