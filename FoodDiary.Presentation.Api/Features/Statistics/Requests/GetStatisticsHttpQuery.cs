using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Statistics.Requests;

public sealed record GetStatisticsHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 1);
