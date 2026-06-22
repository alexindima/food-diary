using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpQueryMappings {
    public static GetConsumptionsQuery ToQuery(this GetConsumptionsHttpQuery query, Guid userId) {
        return new GetConsumptionsQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            query.DateFrom,
            query.DateTo,
            ParseCsv(query.MealTypes),
            NormalizeNonNegative(query.CaloriesFrom),
            NormalizeNonNegative(query.CaloriesTo),
            query.HasImage,
            query.HasAiSession);
    }

    public static GetConsumptionsOverviewQuery ToQuery(this GetConsumptionsOverviewHttpQuery query, Guid userId) {
        return new GetConsumptionsOverviewQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            query.DateFrom,
            query.DateTo,
            Math.Clamp(query.FavoriteLimit, 1, 50),
            ParseCsv(query.MealTypes),
            NormalizeNonNegative(query.CaloriesFrom),
            NormalizeNonNegative(query.CaloriesTo),
            query.HasImage,
            query.HasAiSession);
    }

    public static GetConsumptionByIdQuery ToQuery(this Guid id, Guid userId) {
        return new GetConsumptionByIdQuery(userId, id);
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
