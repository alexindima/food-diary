using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminContentReportsHttpQuery(
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Pending,
        PresentationQueryValues.Reviewed,
        PresentationQueryValues.Dismissed)] string? Status = null,
    int Page = 1,
    int Limit = 20);
