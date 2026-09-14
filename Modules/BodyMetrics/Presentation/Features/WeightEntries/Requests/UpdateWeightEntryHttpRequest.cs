namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Requests;

public sealed record UpdateWeightEntryHttpRequest(
    DateTime Date,
    double WeightKg);
