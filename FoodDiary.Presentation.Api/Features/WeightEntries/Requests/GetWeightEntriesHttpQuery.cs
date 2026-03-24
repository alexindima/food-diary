namespace FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

public sealed record GetWeightEntriesHttpQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int? Limit = null,
    string Sort = "desc");
