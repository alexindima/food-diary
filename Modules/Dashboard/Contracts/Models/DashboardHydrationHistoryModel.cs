namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardHydrationHistoryModel(
    IReadOnlyList<DashboardHydrationEntryModel> Entries,
    bool HasMore) {
    public const int EntryLimit = 10;
}
