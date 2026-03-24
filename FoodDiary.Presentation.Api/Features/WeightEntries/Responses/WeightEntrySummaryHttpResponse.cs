namespace FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

public sealed record WeightEntrySummaryHttpResponse(
    DateTime StartDate,
    DateTime EndDate,
    double AverageWeight);
