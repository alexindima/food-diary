namespace FoodDiary.Application.Dashboard.Services;

public sealed record DashboardSnapshotSections(
    bool IncludeStatistics,
    bool IncludeMeals,
    bool IncludeWeight,
    bool IncludeWaist,
    bool IncludeHydration,
    bool IncludeFasting,
    bool IncludeAdvice,
    bool IncludeLayout,
    bool IncludeExercise,
    bool IncludeTdee,
    bool IncludeCycle) {
    public static DashboardSnapshotSections All { get; } = new(
        IncludeStatistics: true,
        IncludeMeals: true,
        IncludeWeight: true,
        IncludeWaist: true,
        IncludeHydration: true,
        IncludeFasting: true,
        IncludeAdvice: true,
        IncludeLayout: true,
        IncludeExercise: true,
        IncludeTdee: true,
        IncludeCycle: true);
}
