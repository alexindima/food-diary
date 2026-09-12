namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserFastingReminderModel(int ReminderHours, int FollowUpReminderHours);
