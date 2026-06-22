using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public class GetRecipesQueryHandler(
    IRecipeRepository recipeRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetRecipesQuery, Result<PagedResponse<RecipeModel>>> {
    public async Task<Result<PagedResponse<RecipeModel>>> Handle(GetRecipesQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<RecipeModel>>(Errors.Authentication.InvalidToken);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<RecipeModel>>(accessError);
        }

        (IReadOnlyList<(Domain.Entities.Recipes.Recipe Recipe, int UsageCount)> items, int totalItems) = await recipeRepository.GetPagedAsync(
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

        var recipes = items.Select(item => new {
            item.Recipe,
            item.UsageCount,
            IsOwner = item.Recipe.UserId == userId,
        }).ToList();

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<RecipeModel>(
            recipes.ConvertAll(r => r.Recipe.ToModel(r.UsageCount, r.IsOwner)),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
