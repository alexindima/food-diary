using FoodDiary.Modules.Meals.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardMealsHttpResponse(
    IReadOnlyList<MealHttpResponse> Items,
    int Total);
