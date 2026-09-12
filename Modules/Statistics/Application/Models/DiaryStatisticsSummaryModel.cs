namespace FoodDiary.Application.Statistics.Models;

public sealed record DiaryStatisticsSummaryModel(string TimeZoneId, IReadOnlyList<DiaryStatisticsDayModel> Days, double? DailyWaterGoalMl) {
    public int CalendarDays => Days.Count;
    public int DaysWithMeals => Days.Count(day => day.MealCount > 0);
    public double TotalCalories => Days.Sum(day => day.Calories);
    public double TotalProteins => Days.Sum(day => day.Proteins);
    public double TotalFats => Days.Sum(day => day.Fats);
    public double TotalCarbs => Days.Sum(day => day.Carbs);
    public double TotalFiber => Days.Sum(day => day.Fiber);
    public long TotalWaterMl => Days.Sum(day => day.WaterMl);
    public int MealCount => Days.Sum(day => day.MealCount);
    public double AverageCaloriesPerCalendarDay => CalendarDays == 0 ? 0 : TotalCalories / CalendarDays;
}
