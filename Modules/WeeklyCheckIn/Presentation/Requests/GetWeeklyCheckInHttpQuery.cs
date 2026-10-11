using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Requests;

public sealed record GetWeeklyCheckInHttpQuery(DateOnly? WeekStart = null, [MaxLength(100)] string? TimeZoneId = null);
