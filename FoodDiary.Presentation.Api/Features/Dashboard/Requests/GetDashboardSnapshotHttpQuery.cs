namespace FoodDiary.Presentation.Api.Features.Dashboard.Requests;

public sealed record GetDashboardSnapshotHttpQuery(
    DateTime Date,
    int Page = 1,
    int PageSize = 10,
    string Locale = "en",
    int TrendDays = 7);
