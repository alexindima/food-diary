namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

public sealed record WeeklyCheckInHttpResponse(
    WeekSummaryHttpResponse ThisWeek,
    WeekSummaryHttpResponse LastWeek,
    WeekTrendHttpResponse Trends,
    IReadOnlyList<string> Suggestions);
