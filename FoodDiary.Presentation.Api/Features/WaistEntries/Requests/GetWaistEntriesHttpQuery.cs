namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record GetWaistEntriesHttpQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int? Limit = null,
    string Sort = "desc");
