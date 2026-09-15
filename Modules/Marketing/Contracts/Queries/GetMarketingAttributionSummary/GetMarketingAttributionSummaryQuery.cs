using FoodDiary.Modules.Marketing.Contracts.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Marketing.Contracts.Queries.GetMarketingAttributionSummary;

public sealed record GetMarketingAttributionSummaryQuery(int Hours) : IRequest<Result<MarketingAttributionSummaryModel>>;
