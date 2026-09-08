using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminBugReportsHttpQuery([OpenApiNumericRange(1, 10000)] int Page = 1, [OpenApiNumericRange(1, 100)] int Limit = 50,
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, [MaxLength(32)] string? Status = null,
    [MaxLength(320)] string? Search = null, Guid? Id = null);
