namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record GetWeightSummariesHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays = 1);
