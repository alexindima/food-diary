using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecipes;

public record GetRecipesQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    string? Category = null,
    int? MaxTotalTime = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null) : IQuery<Result<PagedResponse<RecipeModel>>>, IUserRequest;
