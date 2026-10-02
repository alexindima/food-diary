using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;

public sealed record ExportCatalogRecipesQuery : IQuery<Result<IReadOnlyList<CatalogRecipeModel>>>;
