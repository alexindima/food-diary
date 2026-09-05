using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record GetWeightSummariesHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 1);
