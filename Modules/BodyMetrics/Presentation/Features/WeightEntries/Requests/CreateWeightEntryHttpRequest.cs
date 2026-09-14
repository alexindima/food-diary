namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Requests;

public sealed record CreateWeightEntryHttpRequest(
    DateTime Date,
    double WeightKg);
