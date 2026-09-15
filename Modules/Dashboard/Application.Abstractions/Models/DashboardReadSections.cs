namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

public sealed record DashboardReadSections(
    bool IncludeStatistics,
    bool IncludeMeals,
    bool IncludeWeight,
    bool IncludeWaist,
    bool IncludeHydration);
