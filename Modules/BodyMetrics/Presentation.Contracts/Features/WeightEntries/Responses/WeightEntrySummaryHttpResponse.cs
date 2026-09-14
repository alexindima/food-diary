namespace FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

public sealed record WeightEntrySummaryHttpResponse(
    DateTime StartDate,
    DateTime EndDate,
    double AverageWeightKg);
