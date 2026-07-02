namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardReadSections(
    bool IncludeStatistics,
    bool IncludeMeals,
    bool IncludeWeight,
    bool IncludeWaist,
    bool IncludeHydration);
