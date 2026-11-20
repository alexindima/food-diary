using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Validation;

using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecipes;

public sealed class GetRecipesQueryHandler(
    IRecipeOverviewReadService recipeOverviewReadService,
    ICurrentUserAccessService currentUserAccessService,
    IRecipePublicationQueue? publicationQueue = null)
    : IQueryHandler<GetRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(GetRecipesQuery query, CancellationToken cancellationToken) {
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<RecipeModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
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
            cancellationToken);

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        RecipeModel[] models = [.. items.Select(recipe => recipe.ToModel())];
        IReadOnlyDictionary<RecipeId, RecipePublicationState> states =
            await RecipePublicationDraft.GetOwnedStatesAsync(models, publicationQueue, cancellationToken);
        var response = new PagedResponse<RecipeModel>(
            models.Select(model => RecipePublicationDraft.WithPublication(model, states)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
