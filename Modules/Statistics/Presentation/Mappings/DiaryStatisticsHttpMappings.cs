using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetDiaryStatistics;
using FoodDiary.Modules.Statistics.Presentation.Requests;
using FoodDiary.Modules.Statistics.Presentation.Responses;

namespace FoodDiary.Modules.Statistics.Presentation.Mappings;

public static class DiaryStatisticsHttpMappings {
    extension(GetDiaryStatisticsHttpQuery query) {
        public GetDiaryStatisticsQuery ToQuery(Guid userId) => new(userId, query.Days);
    }

    extension(DiaryStatisticsSummaryModel model) {
        public DiaryStatisticsSummaryHttpResponse ToHttpResponse() => new(model.TimeZoneId, model.CalendarDays, model.DaysWithMeals,
            model.TotalCalories, model.TotalProteins, model.TotalFats, model.TotalCarbs, model.TotalFiber, model.TotalWaterMl,
            model.MealCount, model.AverageCaloriesPerCalendarDay, model.DailyWaterGoalMl,
            [.. model.Days.Select(day => new DiaryStatisticsDayHttpResponse(day.Date, day.Calories, day.Proteins, day.Fats,
                day.Carbs, day.Fiber, day.WaterMl, day.MealCount, day.CalorieGoal))]);
    }
}
