using FoodDiary.Application.Marketing.Models;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionRange;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminMarketingRangeHttpMappings {
    public static GetMarketingAttributionRangeQuery ToQuery(this GetMarketingAttributionRangeHttpQuery query) =>
        new(query.FromUtc, query.ToUtc, query.Page, query.Limit, query.EventType, query.Channel, query.Search);

    public static MarketingAttributionRangeHttpResponse ToHttpResponse(this MarketingAttributionRangeModel result) =>
        new(result.FromUtc, result.ToUtc, result.PreviousFromUtc, result.Current.ToHttpResponse(), result.Previous.ToHttpResponse(),
            result.ByDay.Select(item => new MarketingAttributionDayHttpResponse(item.Date, item.Visits, item.Signups, item.PremiumStarts)).ToList(), result.EventTotal);
}
