using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;

public sealed class GetMarketingAttributionSummaryQueryHandler(IMarketingAttributionSummaryReadService readService)
    : IQueryHandler<GetMarketingAttributionSummaryQuery, Result<MarketingAttributionSummaryModel>> {
    public async Task<Result<MarketingAttributionSummaryModel>> Handle(
        GetMarketingAttributionSummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetAsync(query.Hours, cancellationToken).ConfigureAwait(false);
    }
}
