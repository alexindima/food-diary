namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Responses;

public sealed record WeeklyCheckInHttpResponse(
    WeekSummaryHttpResponse ThisWeek,
    WeekSummaryHttpResponse LastWeek,
    WeekTrendHttpResponse Trends,
    IReadOnlyList<string> Suggestions);
