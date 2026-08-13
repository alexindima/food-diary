using FoodDiary.Presentation.Api.Features.Meals.Responses;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DashboardMealsHttpResponse(
    IReadOnlyList<MealHttpResponse> Items,
    int Total);
