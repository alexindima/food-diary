using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminImpersonationSessionsHttpQuery(
    int Page = 1,
    int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null);
