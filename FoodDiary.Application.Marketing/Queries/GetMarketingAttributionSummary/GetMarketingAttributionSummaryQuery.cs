using FoodDiary.Application.Marketing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;

public sealed record GetMarketingAttributionSummaryQuery(int Hours) : IRequest<Result<MarketingAttributionSummaryModel>>;
