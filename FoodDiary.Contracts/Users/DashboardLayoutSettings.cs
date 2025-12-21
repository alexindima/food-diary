using System.Collections.Generic;

namespace FoodDiary.Contracts.Users;

public record DashboardLayoutSettings(
    IReadOnlyList<string>? Web,
    IReadOnlyList<string>? Mobile);
