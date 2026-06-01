namespace FoodDiary.Application.Dashboard.Services;

public sealed record DashboardSnapshotRequest(
    Guid UserId,
    DateTime Date,
    DateTime? DateTo,
    string Locale,
    int TrendDays,
    int Page,
    int PageSize,
    DashboardSnapshotSections? Sections = null);
