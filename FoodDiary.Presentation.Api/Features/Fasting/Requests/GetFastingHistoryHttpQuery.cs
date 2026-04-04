namespace FoodDiary.Presentation.Api.Features.Fasting.Requests;

public sealed record GetFastingHistoryHttpQuery(DateTime From, DateTime To);
