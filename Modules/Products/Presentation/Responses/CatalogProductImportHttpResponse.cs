namespace FoodDiary.Modules.Products.Presentation.Responses;

public sealed record CatalogProductImportHttpResponse(
    Guid Id,
    string Status,
    IReadOnlyList<string> Errors);
