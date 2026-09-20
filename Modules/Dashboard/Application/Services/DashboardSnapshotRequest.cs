using FoodDiary.Modules.Dashboard.Application.Models;

namespace FoodDiary.Modules.Dashboard.Application.Services;

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
    DashboardUserContextModel? UserContext = null,
    string? TimeZoneId = null);
