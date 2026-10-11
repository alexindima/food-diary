using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.WeeklyGoals.Presentation.Requests;

public sealed record GetWeeklyGoalHttpQuery(DateOnly WeekStart, [MaxLength(100)] string? TimeZoneId = null);
