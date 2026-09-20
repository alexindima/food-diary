namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

/// <summary>Measurement dates are calendar values encoded at UTC midnight, independent of event bounds.</summary>
public sealed record DashboardCalendarRange(DateTime Date, DateTime DateTo, DateTime TrendDateFrom, TimeZoneInfo TimeZone);
