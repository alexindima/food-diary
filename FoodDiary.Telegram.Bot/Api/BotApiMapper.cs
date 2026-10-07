using FoodDiary.Telegram.Bot.Api.Generated.Model;
using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Api;

internal static class BotApiMapper {
    internal static BotDiaryStatistics Statistics(DiaryStatisticsSummaryHttpResponse value) => new(
        value.TimeZoneId, value.CalendarDays, value.DaysWithMeals, value.TotalCalories, value.TotalProteins,
        value.TotalFats, value.TotalCarbs, value.TotalFiber, value.TotalWaterMl, value.MealCount,
        value.AverageCaloriesPerCalendarDay, value.DailyWaterGoalMl,
        value.Days.Select(day => new BotDiaryStatisticsDay(day.Date, day.Calories, day.Proteins, day.Fats,
            day.Carbs, day.Fiber, day.WaterMl, day.MealCount, day.CalorieGoal)).ToArray());

    internal static BotImageUpload Upload(GetImageUploadUrlHttpResponse value) => new(value.UploadUrl, value.FileUrl, value.ExpiresAtUtc, value.AssetId);

    internal static BotRecognitionJob Recognition(FoodRecognitionJobHttpResponse value) => new(
        value.Id, value.ImageAssetId, value.Status, value.ErrorCode, value.NutritionErrorCode,
        value.Nutrition is null ? null : new BotMealNutrition(value.Nutrition.Calories, value.Nutrition.Protein, value.Nutrition.Fat, value.Nutrition.Carbs));

    internal static BotRecognizedMeal Meal(RecognizedMealCreationHttpResponse value) => new(value.OperationId, value.MealId, value.UndoUntilUtc, value.Undone);

    internal static BotHydrationReceipt Water(HydrationOperationHttpResponse value) => new(value.OperationId, value.EntryId, value.TimestampUtc, value.AmountMl);

    internal static BotOperationLease Lease(TelegramOperationLeaseHttpResponse value) => new(
        value.OperationId, value.LeaseId, value.UserId, value.SecurityVersion, value.Payload, value.Checkpoint, value.LeaseExpiresAtUtc, value.CreatedAtUtc);
}
