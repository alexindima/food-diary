using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecipes;

public record GetRecipesQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic) : IQuery<Result<PagedResponse<RecipeModel>>>, IUserRequest;
