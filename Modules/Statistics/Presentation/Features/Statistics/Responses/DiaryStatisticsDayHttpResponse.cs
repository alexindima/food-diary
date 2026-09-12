namespace FoodDiary.Presentation.Api.Features.Statistics.Responses;

public sealed record DiaryStatisticsDayHttpResponse(DateOnly Date, double Calories, double Proteins, double Fats,
    double Carbs, double Fiber, long WaterMl, int MealCount, double? CalorieGoal);
