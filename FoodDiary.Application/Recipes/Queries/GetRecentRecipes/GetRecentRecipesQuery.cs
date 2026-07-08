using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed record GetRecentRecipesQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<RecipeModel>>>, IUserRequest;
