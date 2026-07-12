using FoodDiary.Application.Marketing.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Common;

public interface IMarketingAttributionSummaryReadService {
    Task<Result<MarketingAttributionSummaryModel>> GetAsync(int hours, CancellationToken cancellationToken);
}
