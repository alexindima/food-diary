using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

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
    User CurrentUser);
