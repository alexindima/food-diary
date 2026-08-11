namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record DashboardLayoutModel(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
