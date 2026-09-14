using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

public sealed record GetWaistSummariesHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 1);
