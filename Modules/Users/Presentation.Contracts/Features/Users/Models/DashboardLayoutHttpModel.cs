namespace FoodDiary.Presentation.Api.Features.Users.Models;

public sealed record DashboardLayoutHttpModel(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
