using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipeCategories;

public sealed record GetPublicRecipeCategoriesQuery(string? Search = null, string? Language = null)
    : IQuery<Result<IReadOnlyList<string>>>;
