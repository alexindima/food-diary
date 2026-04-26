using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.ExploreRecipes;

public record ExploreRecipesQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    string? Category,
    int? MaxPrepTime,
    string SortBy) : IQuery<Result<PagedResponse<RecipeModel>>>, IUserRequest;
