using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Services;

internal sealed record DashboardBuildContext(
    UserId UserId,
    DateTime DayStart,
    DateTime DayEndStart,
    DateTime DayEnd,
    int PeriodDays,
    string Locale,
    int Page,
    int PageSize,
    int TrendDays,
    DateTime TrendStart,
    DashboardSnapshotSections Sections,
    DashboardUserContextModel CurrentUser);
