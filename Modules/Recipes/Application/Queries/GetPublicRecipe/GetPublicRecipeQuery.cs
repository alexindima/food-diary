using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipe;

public sealed record GetPublicRecipeQuery(Guid RecipeId) : IQuery<Result<PublicRecipeModel>>;
