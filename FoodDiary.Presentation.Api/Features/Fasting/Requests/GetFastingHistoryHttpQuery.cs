namespace FoodDiary.Presentation.Api.Features.Fasting.Requests;

public sealed record GetFastingHistoryHttpQuery(DateTime From, DateTime To, int Page = 1, int Limit = 10);
