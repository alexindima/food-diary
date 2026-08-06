namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record GetWeightHistoryPageSummaryHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays = 3,
    int EntriesLimit = 500);
