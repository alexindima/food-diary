using FoodDiary.Application.Marketing.Models;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Common;

public interface IMarketingAttributionRangeReadService {
    Task<Result<MarketingAttributionRangeModel>> GetAsync(GetMarketingAttributionRangeQuery request, CancellationToken cancellationToken);
}
