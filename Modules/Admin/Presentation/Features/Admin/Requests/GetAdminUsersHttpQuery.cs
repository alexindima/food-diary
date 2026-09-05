using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminUsersHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)]
    [AllowedQueryValues(
        PresentationQueryValues.All,
        PresentationQueryValues.Active,
        PresentationQueryValues.Inactive,
        PresentationQueryValues.Deleted)] string? Status = null,
    bool IncludeDeleted = false);
