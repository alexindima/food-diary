using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetMarketingAttributionRangeHttpQuery(DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    [OpenApiNumericRange(1, 10000)] int Page = 1, [OpenApiNumericRange(1, 100)] int Limit = 50,
    [MaxLength(32)] string? EventType = null, [MaxLength(16)] string? Channel = null, [MaxLength(320)] string? Search = null);
