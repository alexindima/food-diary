namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminRetentionDayHttpResponse(DateTime Date, int ActiveUsers, int MealEntries = 0);
