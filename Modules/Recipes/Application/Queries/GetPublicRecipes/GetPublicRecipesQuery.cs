using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;

public sealed record GetPublicRecipesQuery(int Page, int Limit, string? Search, string? Category, int? MaxTotalTime, string SortBy = "newest")
    : IQuery<Result<PagedResponse<PublicRecipeModel>>>;
