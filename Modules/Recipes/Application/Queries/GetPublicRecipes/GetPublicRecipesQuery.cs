using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;

public sealed record GetPublicRecipesQuery(int Page, int Limit, string? Search, string? Category, int? MaxTotalTime)
    : IQuery<Result<PagedResponse<PublicRecipeModel>>>;
