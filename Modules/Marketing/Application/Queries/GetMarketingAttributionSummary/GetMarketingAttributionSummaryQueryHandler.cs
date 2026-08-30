using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;

public sealed class GetMarketingAttributionSummaryQueryHandler(IMarketingAttributionSummaryReadService readService)
    : IRequestHandler<GetMarketingAttributionSummaryQuery, Result<MarketingAttributionSummaryModel>> {
    public async Task<Result<MarketingAttributionSummaryModel>> Handle(
        GetMarketingAttributionSummaryQuery request,
        CancellationToken cancellationToken) {
        return await readService.GetAsync(request.Hours, cancellationToken).ConfigureAwait(false);
    }
}
