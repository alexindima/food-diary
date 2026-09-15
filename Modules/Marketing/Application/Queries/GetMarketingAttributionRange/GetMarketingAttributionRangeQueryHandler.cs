using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Contracts.Models;
using FoodDiary.Modules.Marketing.Contracts.Queries.GetMarketingAttributionRange;
using FoodDiary.Results;
using FoodDiary.Modules.Marketing.Application.Mappings;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Marketing.Application.Queries.GetMarketingAttributionRange;

public sealed class GetMarketingAttributionRangeQueryHandler(IMarketingAttributionRangeReadRepository repository, TimeProvider timeProvider)
    : IRequestHandler<GetMarketingAttributionRangeQuery, Result<MarketingAttributionRangeModel>> {
    public Task<Result<MarketingAttributionRangeModel>> Handle(GetMarketingAttributionRangeQuery request, CancellationToken cancellationToken) =>
        GetAsync(request, cancellationToken);
    private async Task<Result<MarketingAttributionRangeModel>> GetAsync(GetMarketingAttributionRangeQuery request, CancellationToken cancellationToken) {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (request.FromUtc < DateTimeOffset.UnixEpoch || request.FromUtc >= request.ToUtc || request.ToUtc.UtcDateTime > now.Date.AddDays(1) ||
            request.Page is < 1 or > 10000 || request.Limit is < 1 or > 100 || request.Search?.Length > 320 ||
            request.EventType is not (null or "" or "page_landing" or "signup_completed" or "premium_started") ||
            request.Channel is not (null or "" or "tracked" or "direct")) {
            return Result.Failure<MarketingAttributionRangeModel>(Errors.Validation.Invalid("Range", "Invalid reporting period or event filters."));
        }
        var filter = new MarketingAttributionRangeFilter(request.FromUtc.UtcDateTime, request.ToUtc.UtcDateTime,
            request.Page, request.Limit, request.EventType, request.Channel, request.Search);
        MarketingAttributionRangeRecord result = await repository.GetRangeAsync(filter, cancellationToken).ConfigureAwait(false);
        int hours = (int)Math.Ceiling((request.ToUtc - request.FromUtc).TotalHours);
        return Result.Success(new MarketingAttributionRangeModel(filter.FromUtc, filter.ToUtc, filter.FromUtc - (filter.ToUtc - filter.FromUtc),
            MarketingAttributionMappings.ToSummaryModel(result.Current, hours, now),
            MarketingAttributionMappings.ToSummaryModel(result.Previous, hours, now),
            result.ByDay.Select(item => new MarketingAttributionDayModel(item.Date, item.Visits, item.Signups, item.PremiumStarts)).ToList(), result.EventTotal));
    }
}
