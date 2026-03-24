namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record GetWaistSummariesHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays = 1);
