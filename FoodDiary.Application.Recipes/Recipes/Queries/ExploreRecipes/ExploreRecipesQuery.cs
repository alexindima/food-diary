using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Recipes.Recipes.Models;

namespace FoodDiary.Application.Recipes.Recipes.Queries.ExploreRecipes;

public record ExploreRecipesQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    string? Category,
    int? MaxPrepTime,
    string SortBy) : IQuery<Result<PagedResponse<RecipeModel>>>, IUserRequest;
