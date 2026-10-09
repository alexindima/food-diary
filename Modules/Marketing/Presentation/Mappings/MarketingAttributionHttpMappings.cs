using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Modules.Marketing.Application.Commands.RecordMarketingAttribution;
using FoodDiary.Modules.Marketing.Application.Common;
using FoodDiary.Modules.Marketing.Presentation.Requests;

namespace FoodDiary.Modules.Marketing.Presentation.Mappings;

public static class MarketingAttributionHttpMappings {
    extension(MarketingAttributionHttpRequest request) {
        public RecordMarketingAttributionCommand ToCommand(Guid eventId) {
            return new RecordMarketingAttributionCommand(
                MarketingAttributionEventTypes.PageLanding,
                request.Timestamp,
                UserId: null,
                new AnonymousVisitorId(request.AnonymousId),
                new MarketingSessionId(request.SessionId),
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
                new UserId(userId),
                new AnonymousVisitorId(request.AnonymousId),
                new MarketingSessionId(request.SessionId),
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
