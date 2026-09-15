using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Marketing.Application.Mappings;
using FoodDiary.Modules.Marketing.Contracts.Queries.GetMarketingAttributionSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Marketing.Application.Queries.GetMarketingAttributionSummary;

public sealed class GetMarketingAttributionSummaryQueryHandler(IMarketingAttributionEventReadRepository repository,
    TimeProvider timeProvider)
    : IRequestHandler<GetMarketingAttributionSummaryQuery, Result<MarketingAttributionSummaryModel>> {
    public async Task<Result<MarketingAttributionSummaryModel>> Handle(
        GetMarketingAttributionSummaryQuery request,
        CancellationToken cancellationToken) {
        return await GetAsync(request.Hours, cancellationToken).ConfigureAwait(false);
    }
    private async Task<Result<MarketingAttributionSummaryModel>> GetAsync(int hours, CancellationToken cancellationToken) {
        int normalizedWindowHours = Math.Clamp(hours, 1, 2160);
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        DateTime windowStartUtc = nowUtc.AddHours(-normalizedWindowHours);
        MarketingAttributionSummaryRecord summary = await repository.GetSummaryAsync(windowStartUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success(MarketingAttributionMappings.ToSummaryModel(summary, normalizedWindowHours, nowUtc));
    }
}
