namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Requests;

public sealed record GetWeeklyCheckInHttpQuery(DateOnly? WeekStart = null);
