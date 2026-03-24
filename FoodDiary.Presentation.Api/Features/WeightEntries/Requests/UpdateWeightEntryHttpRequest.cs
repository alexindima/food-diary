namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record UpdateWeightEntryHttpRequest(
    DateTime Date,
    double Weight);
