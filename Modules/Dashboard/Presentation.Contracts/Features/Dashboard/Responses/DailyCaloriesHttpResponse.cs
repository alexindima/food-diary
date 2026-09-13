namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DailyCaloriesHttpResponse(
    DateTime Date,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber);
