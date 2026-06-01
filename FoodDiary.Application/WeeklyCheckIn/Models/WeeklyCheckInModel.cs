namespace FoodDiary.Application.WeeklyCheckIn.Models;

public sealed record WeeklyCheckInModel(
    WeekSummaryModel ThisWeek,
    WeekSummaryModel LastWeek,
    WeekTrendModel Trends,
    IReadOnlyList<string> Suggestions);
