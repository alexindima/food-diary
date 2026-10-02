namespace FoodDiary.Modules.Products.Application.Models;

public sealed record CatalogProductImportResult(Guid Id, string Status, IReadOnlyList<string> Errors);
