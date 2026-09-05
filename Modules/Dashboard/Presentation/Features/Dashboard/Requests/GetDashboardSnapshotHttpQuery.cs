using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Requests;

public sealed record GetDashboardSnapshotHttpQuery(
    DateTime Date,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int PageSize = 10,
    [Required, MaxLength(PresentationQueryLimits.MaximumLocaleLength)] string Locale = "en",
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumDashboardTrendDays)] int TrendDays = 7,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumTimeZoneOffsetMinutes, PresentationQueryLimits.MaximumTimeZoneOffsetMinutes)] int? TimeZoneOffsetMinutes = null);
