namespace FoodDiary.Presentation.Api.Features.Dashboard.Requests;

public sealed record GetDailyAdviceHttpQuery(
    DateTime Date,
    string Locale = "en");
