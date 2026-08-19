using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

public sealed record GetWaistEntriesHttpQuery(
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int? Limit = null,
    [Required, MaxLength(PresentationQueryLimits.MaximumSortLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Ascending,
        PresentationQueryValues.Descending)] string Sort = "desc");
