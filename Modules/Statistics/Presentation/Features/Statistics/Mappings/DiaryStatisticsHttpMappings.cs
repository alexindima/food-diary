using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetDiaryStatistics;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

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
