using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;

public sealed record ImportCatalogRecipeCommand(Guid UserId, CatalogRecipeModel Recipe, bool Preview)
    : IRequest<Result<CatalogRecipeImportResult>>;
