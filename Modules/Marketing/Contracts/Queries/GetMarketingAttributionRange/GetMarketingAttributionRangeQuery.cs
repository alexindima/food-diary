using FoodDiary.Modules.Marketing.Contracts.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Marketing.Contracts.Queries.GetMarketingAttributionRange;

public sealed record GetMarketingAttributionRangeQuery(DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int Page = 1, int Limit = 50, string? EventType = null, string? Channel = null, string? Search = null)
    : IRequest<Result<MarketingAttributionRangeModel>>;
