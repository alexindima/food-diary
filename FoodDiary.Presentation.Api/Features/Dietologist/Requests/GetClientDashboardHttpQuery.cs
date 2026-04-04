namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record GetClientDashboardHttpQuery(
    DateTime Date,
    int Page = 1,
    int PageSize = 10,
    string Locale = "en",
    int TrendDays = 7);
