using FoodDiary.Presentation.Api.Features.Meals.Responses;

namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardMealsHttpResponse(
    IReadOnlyList<MealHttpResponse> Items,
    int Total);
