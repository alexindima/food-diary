namespace FoodDiary.Application.Users.Models;

public sealed record DashboardLayoutModel(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
