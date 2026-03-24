using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsWithRecent;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Products.Requests;

namespace FoodDiary.Presentation.Api.Features.Products.Mappings;

public static class ProductHttpQueryMappings {
    public static GetProductsQuery ToQuery(this GetProductsHttpQuery query, UserId userId) {
        return new GetProductsQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            SanitizeSearch(query.Search),
            query.IncludePublic,
            ParseProductTypes(query.ProductTypes));
    }

    public static GetProductsWithRecentQuery ToQuery(this GetProductsWithRecentHttpQuery query, UserId userId) {
        return new GetProductsWithRecentQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            SanitizeSearch(query.Search),
            query.IncludePublic,
            Math.Clamp(query.RecentLimit, 1, 50),
            ParseProductTypes(query.ProductTypes));
    }

    public static GetRecentProductsQuery ToQuery(this GetRecentProductsHttpQuery query, UserId userId) {
        return new GetRecentProductsQuery(userId, Math.Clamp(query.Limit, 1, 50), query.IncludePublic);
    }

    private static string? SanitizeSearch(string? search) {
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    }

    private static IReadOnlyCollection<string>? ParseProductTypes(string? productTypes) {
        if (string.IsNullOrWhiteSpace(productTypes)) {
            return null;
        }

        var values = productTypes
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length > 0 ? values : null;
    }
}
