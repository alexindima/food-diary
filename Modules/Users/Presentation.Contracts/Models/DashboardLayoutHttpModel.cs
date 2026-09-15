namespace FoodDiary.Modules.Users.Presentation.Contracts.Models;

public sealed record DashboardLayoutHttpModel(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
