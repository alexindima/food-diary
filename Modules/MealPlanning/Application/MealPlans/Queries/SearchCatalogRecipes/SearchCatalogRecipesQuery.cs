using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;

public sealed record SearchCatalogRecipesQuery(string? Search, int Limit = 100) : IQuery<Result<IReadOnlyList<CatalogRecipeReadModel>>>;
