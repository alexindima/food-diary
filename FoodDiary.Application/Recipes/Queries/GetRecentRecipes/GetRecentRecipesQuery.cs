using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed record GetRecentRecipesQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<RecipeModel>>>, IUserRequest;
