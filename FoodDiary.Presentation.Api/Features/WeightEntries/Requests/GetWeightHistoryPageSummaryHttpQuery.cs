using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record GetWeightHistoryPageSummaryHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 3,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumHistoryEntries)] int EntriesLimit = 500);
