namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record GoalHistoryPageModel<T>(IReadOnlyList<T> Items, string? NextCursor);
