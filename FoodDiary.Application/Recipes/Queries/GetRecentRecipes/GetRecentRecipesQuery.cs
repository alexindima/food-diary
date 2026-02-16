using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Queries.GetRecentRecipes;

public sealed record GetRecentRecipesQuery(UserId? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<RecipeResponse>>>;
