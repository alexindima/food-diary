using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Queries.ExploreRecipes;

public record ExploreRecipesQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    string? Category,
    int? MaxPrepTime,
    string SortBy) : IQuery<Result<PagedResponse<RecipeModel>>>, IUserRequest;
