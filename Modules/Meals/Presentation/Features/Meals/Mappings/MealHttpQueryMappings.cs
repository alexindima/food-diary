using FoodDiary.Application.Meals.Queries.GetMeals;
using FoodDiary.Application.Meals.Queries.GetMealsOverview;
using FoodDiary.Application.Meals.Queries.GetMealById;
using FoodDiary.Presentation.Api.Features.Meals.Requests;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class MealHttpQueryMappings {
    extension(GetMealsHttpQuery query) {
        public GetMealsQuery ToQuery(Guid userId) {
            return new GetMealsQuery(
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
    }

    extension(GetMealsOverviewHttpQuery query) {
        public GetMealsOverviewQuery ToQuery(Guid userId) {
            return new GetMealsOverviewQuery(
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
    }

    extension(Guid id) {
        public GetMealByIdQuery ToQuery(Guid userId) {
            return new GetMealByIdQuery(userId, id);
        }
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
