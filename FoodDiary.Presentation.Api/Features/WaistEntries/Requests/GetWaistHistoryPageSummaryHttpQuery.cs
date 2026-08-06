namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record GetWaistHistoryPageSummaryHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays = 3,
    int EntriesLimit = 500);
