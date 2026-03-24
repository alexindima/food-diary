namespace FoodDiary.Presentation.Api.Features.Statistics.Requests;

public sealed record GetStatisticsHttpQuery(
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays = 1);
