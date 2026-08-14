using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Recipes.Models;

namespace FoodDiary.Application.Recipes.Recipes.Queries.GetRecentRecipes;

public sealed record GetRecentRecipesQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<RecipeModel>>>, IUserRequest;
