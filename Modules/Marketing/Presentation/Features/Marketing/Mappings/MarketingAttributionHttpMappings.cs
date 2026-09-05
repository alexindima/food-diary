using FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;

namespace FoodDiary.Presentation.Api.Features.Marketing.Mappings;

public static class MarketingAttributionHttpMappings {
    extension(MarketingAttributionHttpRequest request) {
        public RecordMarketingAttributionCommand ToCommand(Guid eventId) {
            return new RecordMarketingAttributionCommand(
                MarketingAttributionEventTypes.PageLanding,
                request.Timestamp,
                UserId: null,
                request.AnonymousId,
                request.SessionId,
                request.LandingPath,
                request.ReferrerHost,
                request.UtmSource,
                request.UtmMedium,
                request.UtmCampaign,
                request.UtmContent,
                request.UtmTerm,
                request.BuildVersion,
                eventId);
        }
    }

    extension(MarketingSignupAttributionHttpRequest request) {
        public RecordMarketingAttributionCommand ToCommand(Guid userId, Guid eventId) {
            return new RecordMarketingAttributionCommand(
                MarketingAttributionEventTypes.SignupCompleted,
                request.Timestamp,
                userId,
                request.AnonymousId,
                request.SessionId,
                request.LandingPath,
                request.ReferrerHost,
                request.UtmSource,
                request.UtmMedium,
                request.UtmCampaign,
                request.UtmContent,
                request.UtmTerm,
                request.BuildVersion,
                eventId);
        }
    }
}
