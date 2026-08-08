using FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;

namespace FoodDiary.Presentation.Api.Features.Marketing.Mappings;

public static class MarketingAttributionHttpMappings {
    extension(MarketingAttributionHttpRequest request) {
        public RecordMarketingAttributionCommand ToCommand(Guid? userId = null) {
            return new RecordMarketingAttributionCommand(
                request.EventType,
                request.Timestamp,
                userId ?? request.UserId,
                request.AnonymousId,
                request.SessionId,
                request.LandingPath,
                request.ReferrerHost,
                request.UtmSource,
                request.UtmMedium,
                request.UtmCampaign,
                request.UtmContent,
                request.UtmTerm,
                request.BuildVersion);
        }
    }
}
