using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;

public sealed class SearchCatalogRecipesQueryHandler(IMealPlanCatalogRecipeReader reader)
    : IQueryHandler<SearchCatalogRecipesQuery, Result<IReadOnlyList<CatalogRecipeReadModel>>> {
    public async Task<Result<IReadOnlyList<CatalogRecipeReadModel>>> Handle(SearchCatalogRecipesQuery query, CancellationToken cancellationToken) =>
        Result.Success(await reader.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false));
}
