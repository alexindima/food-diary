using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetFastingTelemetrySummaryHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumFastingTelemetryHours)] int Hours = 24);
