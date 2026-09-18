namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminRetentionDay(DateTime Date, int ActiveUsers, int MealEntries = 0);
