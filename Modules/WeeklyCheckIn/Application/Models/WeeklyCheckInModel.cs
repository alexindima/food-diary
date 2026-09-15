namespace FoodDiary.Modules.WeeklyCheckIn.Application.Models;

public sealed record WeeklyCheckInModel(
    WeekSummaryModel ThisWeek,
    WeekSummaryModel LastWeek,
    WeekTrendModel Trends,
    IReadOnlyList<string> Suggestions);
