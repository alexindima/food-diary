using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;

public sealed class GetPublicRecipeCategoriesQueryHandler(IRecipeOverviewReadService recipes)
    : IQueryHandler<GetPublicRecipeCategoriesQuery, Result<IReadOnlyList<string>>> {
    public async Task<Result<IReadOnlyList<string>>> Handle(GetPublicRecipeCategoriesQuery query, CancellationToken cancellationToken) =>
        Result.Success(await recipes.GetPublicCategoriesAsync(query.Search, query.Language, cancellationToken).ConfigureAwait(false));
}
