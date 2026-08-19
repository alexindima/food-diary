using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Requests;

public sealed record GetDashboardSnapshotHttpQuery(
    DateTime Date,
    int Page = 1,
    int PageSize = 10,
    [Required, MaxLength(PresentationQueryLimits.MaximumLocaleLength)] string Locale = "en",
    int TrendDays = 7,
    int? TimeZoneOffsetMinutes = null);
