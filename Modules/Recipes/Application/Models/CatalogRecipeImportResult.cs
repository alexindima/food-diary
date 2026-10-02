namespace FoodDiary.Modules.Recipes.Application.Models;

public sealed record CatalogRecipeImportResult(Guid Id, string Status, IReadOnlyList<string> Errors);
