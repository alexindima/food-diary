using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Marketing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange;

public sealed class GetMarketingAttributionRangeQueryHandler(IMarketingAttributionRangeReadService service)
    : IRequestHandler<GetMarketingAttributionRangeQuery, Result<MarketingAttributionRangeModel>> {
    public Task<Result<MarketingAttributionRangeModel>> Handle(GetMarketingAttributionRangeQuery request, CancellationToken cancellationToken) =>
        service.GetAsync(request, cancellationToken);
}
