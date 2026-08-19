using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminBillingHttpQuery(
    int Page = 1,
    int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Provider = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Status = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Kind = null,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
