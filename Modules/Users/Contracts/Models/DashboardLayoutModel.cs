namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record DashboardLayoutModel(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
