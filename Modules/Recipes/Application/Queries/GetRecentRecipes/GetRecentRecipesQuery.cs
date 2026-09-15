using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Application.Models;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetRecentRecipes;

public sealed record GetRecentRecipesQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<RecipeModel>>>, IUserRequest;
