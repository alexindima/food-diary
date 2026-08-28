using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.ExploreRecipes;

public sealed class ExploreRecipesQueryHandler(IRecipeOverviewReadService recipeOverviewReadService)
    : IQueryHandler<ExploreRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(
        ExploreRecipesQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1, maxPageSize: 50);

        Result<UserId> currentUserIdResult = ResolveCurrentUserId(query);
        if (currentUserIdResult.IsFailure) {
            return UserIdParser.ToFailure<PagedResponse<RecipeModel>>(currentUserIdResult);
        }

        (IReadOnlyList<RecipeOverviewReadItem> items, int totalItems) = await recipeOverviewReadService.GetExplorePagedAsync(
            currentUserIdResult.Value, pageNumber, pageSize, query.Search, query.Category,
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

    private static Result<UserId> ResolveCurrentUserId(ExploreRecipesQuery query) =>
        query.UserId.HasValue
            ? UserIdParser.Parse(
                query.UserId.Value,
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."))
            : Result.Success(UserId.Empty);
}
