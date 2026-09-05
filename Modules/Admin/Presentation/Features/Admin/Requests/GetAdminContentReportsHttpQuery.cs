using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminContentReportsHttpQuery(
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Pending,
        PresentationQueryValues.Reviewed,
        PresentationQueryValues.Dismissed)] string? Status = null,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 20);
