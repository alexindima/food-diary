using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

public sealed record GetWaistHistoryPageSummaryHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumQuantizationDays)] int QuantizationDays = 3,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumHistoryEntries)] int EntriesLimit = 500);
