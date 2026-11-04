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

    internal static BotImageUpload Upload(GetImageUploadUrlHttpResponse value) => new(value.UploadUrl, value.FileUrl, value.ExpiresAtUtc, new BotImageAssetId(value.AssetId));

    internal static BotRecognitionJob Recognition(FoodRecognitionJobHttpResponse value) => new(
        new BotRecognitionId(value.Id), new BotImageAssetId(value.ImageAssetId), value.Status, value.ErrorCode, value.NutritionErrorCode,
        RecognitionNutrition(value.Nutrition, value.Vision));

    private static BotMealNutrition? RecognitionNutrition(FoodNutritionHttpResponse? nutrition, FoodVisionHttpResponse? vision) {
        if (nutrition is null) {
            return null;
        }
        ILookup<string, string> localNames = (vision?.Items ?? [])
            .Select(item => (English: DisplayName(item?.NameEn), Local: DisplayName(item?.NameLocal)))
            .Where(item => item.English is not null && item.Local is not null)
            .ToLookup(item => item.English!, item => item.Local!, StringComparer.OrdinalIgnoreCase);
        BotMealNutrition.Item[]? items = nutrition.Items?
            .Where(item => item is not null)
            .Select(item => (Item: item, Name: DisplayName(item.Name)))
            .Where(item => item.Name is not null)
            .Take(BotMealNutrition.MaxListedItems + 1)
            .Select(item => new BotMealNutrition.Item(item.Name!, localNames[item.Name!].FirstOrDefault(), item.Item.Amount,
                DisplayUnit(item.Item.Unit), item.Item.Calories))
            .ToArray();
        return new BotMealNutrition(nutrition.Calories, nutrition.Protein, nutrition.Fat, nutrition.Carbs,
            Items: items is { Length: > 0 } ? items : null);
    }

    private static string? DisplayName(string? value) {
        string? name = value?.Trim().ReplaceLineEndings(" ");
        return name is { Length: > 0 and <= BotMealNutrition.MaxListedItemLength } ? name : null;
    }

    private static string? DisplayUnit(string? value) {
        string? unit = value?.Trim().ReplaceLineEndings(" ");
        return unit is { Length: > 0 and <= 16 } ? unit : null;
    }

    internal static BotRecognizedMeal Meal(RecognizedMealCreationHttpResponse value) => new(new BotOperationId(value.OperationId), new BotMealId(value.MealId), value.UndoUntilUtc, value.Undone);

    internal static BotHydrationReceipt Water(HydrationOperationHttpResponse value) => new(new BotOperationId(value.OperationId), new BotHydrationEntryId(value.EntryId), value.TimestampUtc, value.AmountMl);

    internal static BotOperationLease Lease(TelegramOperationLeaseHttpResponse value) => new(
        new BotOperationId(value.OperationId), new BotLeaseId(value.LeaseId), new BotUserId(value.UserId), value.SecurityVersion, value.Payload, value.Checkpoint, value.LeaseExpiresAtUtc, value.CreatedAtUtc);
}
