namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DailyCaloriesHttpResponse(
    DateTime Date,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber);
