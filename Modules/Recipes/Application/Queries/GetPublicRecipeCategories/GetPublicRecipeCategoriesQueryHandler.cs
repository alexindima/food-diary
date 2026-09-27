using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;

public sealed class GetPublicRecipeCategoriesQueryHandler : IQueryHandler<GetPublicRecipeCategoriesQuery, Result<IReadOnlyList<string>>> {
    public Task<Result<IReadOnlyList<string>>> Handle(GetPublicRecipeCategoriesQuery query, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> categories = string.IsNullOrWhiteSpace(query.Search)
            ? RecipeCategoryCodes.All
            : RecipeCategoryCodes.All.Where(code => code.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        return Task.FromResult(Result.Success(categories));
    }
}
