using FoodDiary.Application.Products.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Products.Queries.GetRecentProducts;
using FoodDiary.Presentation.Api.Features.Products.Requests;

namespace FoodDiary.Presentation.Api.Features.Products.Mappings;

public static class ProductHttpQueryMappings {
    extension(GetProductsHttpQuery query) {
        public GetProductsQuery ToQuery(Guid userId) {
            return new GetProductsQuery(
                userId,
                Math.Max(query.Page, 1),
                Math.Clamp(query.Limit, 1, 100),
                SanitizeSearch(query.Search),
                query.IncludePublic,
                ParseCsv(query.ProductTypes),
                NormalizeNonNegative(query.CaloriesFrom),
                NormalizeNonNegative(query.CaloriesTo),
                query.HasImage);
        }
    }

    extension(GetProductsOverviewHttpQuery query) {
        public GetProductsOverviewQuery ToQuery(Guid userId) {
            return new GetProductsOverviewQuery(
                userId,
                Math.Max(query.Page, 1),
                Math.Clamp(query.Limit, 1, 100),
                SanitizeSearch(query.Search),
                query.IncludePublic,
                Math.Clamp(query.RecentLimit, 1, 50),
                Math.Clamp(query.FavoriteLimit, 1, 50),
                ParseCsv(query.ProductTypes),
                NormalizeNonNegative(query.CaloriesFrom),
                NormalizeNonNegative(query.CaloriesTo),
                query.HasImage);
        }
    }

    extension(GetRecentProductsHttpQuery query) {
        public GetRecentProductsQuery ToQuery(Guid userId) {
            return new GetRecentProductsQuery(userId, Math.Clamp(query.Limit, 1, 50), query.IncludePublic);
        }
    }

    extension(Guid id) {
        public GetProductByIdQuery ToQuery(Guid userId) {
            return new GetProductByIdQuery(userId, id);
        }
    }

    private static string? SanitizeSearch(string? search) {
        return string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    }

    private static IReadOnlyCollection<string>? ParseCsv(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string[] values = [.. value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        return values.Length > 0 ? values : null;
    }

    private static double? NormalizeNonNegative(double? value) =>
        value is >= 0 ? value : null;
}
