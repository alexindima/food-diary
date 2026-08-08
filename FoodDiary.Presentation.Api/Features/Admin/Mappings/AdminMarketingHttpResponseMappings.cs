using FoodDiary.Application.Marketing.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminMarketingHttpResponseMappings {
    extension(MarketingAttributionSummaryModel summary) {
        public MarketingAttributionSummaryHttpResponse ToHttpResponse() {
            return new MarketingAttributionSummaryHttpResponse(
                summary.WindowHours,
                summary.GeneratedAtUtc,
                summary.Events,
                summary.Visits,
                summary.Signups,
                summary.PremiumStarts,
                summary.AnonymousVisitors,
                summary.Sessions,
                summary.AttributedEvents,
                summary.OrganicEvents,
                summary.AttributedVisits,
                summary.OrganicVisits,
                summary.SignupRatePercent,
                summary.PremiumRatePercent,
                summary.LastEventAtUtc,
                summary.TopCampaigns.Select(static x => x.ToHttpResponse()).ToList(),
                summary.TopSources.Select(static x => x.ToHttpResponse()).ToList(),
                summary.RecentEvents.Select(static x => x.ToHttpResponse()).ToList());
        }
    }

    extension(MarketingAttributionBreakdownModel breakdown) {
        private MarketingAttributionBreakdownHttpResponse ToHttpResponse() {
            return new MarketingAttributionBreakdownHttpResponse(
                breakdown.Source,
                breakdown.Medium,
                breakdown.Campaign,
                breakdown.Events,
                breakdown.Visits,
                breakdown.Signups,
                breakdown.PremiumStarts,
                breakdown.AnonymousVisitors,
                breakdown.Sessions,
                breakdown.SignupRatePercent,
                breakdown.PremiumRatePercent,
                breakdown.LastEventAtUtc);
        }
    }

    extension(MarketingAttributionRecentEventModel recentEvent) {
        private MarketingAttributionRecentEventHttpResponse ToHttpResponse() {
            return new MarketingAttributionRecentEventHttpResponse(
                recentEvent.OccurredAtUtc,
                recentEvent.EventType,
                recentEvent.AnonymousId,
                recentEvent.SessionId,
                recentEvent.LandingPath,
                recentEvent.ReferrerHost,
                recentEvent.UtmSource,
                recentEvent.UtmMedium,
                recentEvent.UtmCampaign,
                recentEvent.UtmContent,
                recentEvent.UtmTerm,
                recentEvent.BuildVersion);
        }
    }
}
