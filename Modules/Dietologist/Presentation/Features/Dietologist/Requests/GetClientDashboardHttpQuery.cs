using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record GetClientDashboardHttpQuery(
    DateTime? Date = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int PageSize = 10,
    [Required, MaxLength(PresentationQueryLimits.MaximumLocaleLength)] string Locale = "en",
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumDashboardTrendDays)] int TrendDays = 7);
