namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record CreateWeightEntryHttpRequest(
    DateTime Date,
    double Weight);
