namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBugReportPage(IReadOnlyList<AdminBugReportEntry> Items, long TotalItems) {
    public bool IsConfigured { get; init; } = true;
}
