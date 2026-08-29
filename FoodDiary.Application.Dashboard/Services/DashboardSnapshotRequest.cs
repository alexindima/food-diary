using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dashboard.Services;

public sealed record DashboardSnapshotRequest(
    Guid UserId,
    DateTime Date,
    DateTime? DateTo,
    string Locale,
    int TrendDays,
    int Page,
    int PageSize,
    DashboardSnapshotSections? Sections = null,
    int? TimeZoneOffsetMinutes = null,
    DashboardUserContextModel? UserContext = null);
