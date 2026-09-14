using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Requests;

public sealed record GetWeightSummariesHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 1);
