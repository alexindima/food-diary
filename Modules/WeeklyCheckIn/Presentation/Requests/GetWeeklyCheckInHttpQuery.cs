namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Requests;

public sealed record GetWeeklyCheckInHttpQuery(DateOnly? WeekStart = null);
