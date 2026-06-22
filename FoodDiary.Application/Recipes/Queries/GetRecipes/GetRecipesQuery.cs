using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

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
