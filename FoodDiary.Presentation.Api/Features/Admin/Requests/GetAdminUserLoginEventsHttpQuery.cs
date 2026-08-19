using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminUserLoginEventsHttpQuery(
    int Page = 1,
    int Limit = 20,
    Guid? UserId = null,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null);
