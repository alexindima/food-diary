using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;

public sealed record GetMarketingAttributionSummaryQuery(int Hours) : IQuery<Result<MarketingAttributionSummaryModel>>;
