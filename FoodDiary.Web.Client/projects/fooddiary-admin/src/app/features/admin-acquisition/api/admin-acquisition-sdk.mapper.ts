import type { MarketingAttributionBreakdownHttpResponse } from '../../../shared/api/sdk/generated/model/marketing-attribution-breakdown-http-response';
import type { MarketingAttributionDayHttpResponse } from '../../../shared/api/sdk/generated/model/marketing-attribution-day-http-response';
import type { MarketingAttributionRangeHttpResponse } from '../../../shared/api/sdk/generated/model/marketing-attribution-range-http-response';
import type { MarketingAttributionRecentEventHttpResponse } from '../../../shared/api/sdk/generated/model/marketing-attribution-recent-event-http-response';
import type { MarketingAttributionSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/marketing-attribution-summary-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type {
    MarketingAttributionBreakdown,
    MarketingAttributionRecentEvent,
    MarketingAttributionSummary,
} from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';

export function marketingAttributionSummaryFromSdk(response: MarketingAttributionSummaryHttpResponse): MarketingAttributionSummary {
    const value = requireSdkFields(response, [
        'windowHours',
        'generatedAtUtc',
        'events',
        'visits',
        'signups',
        'premiumStarts',
        'anonymousVisitors',
        'sessions',
        'attributedEvents',
        'organicEvents',
        'attributedVisits',
        'organicVisits',
        'signupRatePercent',
        'premiumRatePercent',
        'topCampaigns',
        'topSources',
        'recentEvents',
    ]);
    return {
        ...value,
        lastEventAtUtc: value.lastEventAtUtc ?? null,
        topCampaigns: value.topCampaigns.map(item => marketingAttributionBreakdownFromSdk(item)),
        topSources: value.topSources.map(item => marketingAttributionBreakdownFromSdk(item)),
        recentEvents: value.recentEvents.map(item => marketingAttributionRecentEventFromSdk(item)),
    };
}

export function marketingAttributionBreakdownFromSdk(response: MarketingAttributionBreakdownHttpResponse): MarketingAttributionBreakdown {
    const value = requireSdkFields(response, [
        'source',
        'medium',
        'campaign',
        'events',
        'visits',
        'signups',
        'premiumStarts',
        'anonymousVisitors',
        'sessions',
        'signupRatePercent',
        'premiumRatePercent',
    ]);
    return { ...value, lastEventAtUtc: value.lastEventAtUtc ?? null };
}

export function marketingAttributionRecentEventFromSdk(
    response: MarketingAttributionRecentEventHttpResponse,
): MarketingAttributionRecentEvent {
    const value = requireSdkFields(response, ['occurredAtUtc', 'eventType', 'anonymousId', 'sessionId', 'landingPath']);
    return {
        ...value,
        referrerHost: value.referrerHost ?? null,
        utmSource: value.utmSource ?? null,
        utmMedium: value.utmMedium ?? null,
        utmCampaign: value.utmCampaign ?? null,
        utmContent: value.utmContent ?? null,
        utmTerm: value.utmTerm ?? null,
        buildVersion: value.buildVersion ?? null,
    };
}

export function marketingAttributionRangeFromSdk(response: MarketingAttributionRangeHttpResponse): MarketingAttributionRange {
    const value = requireSdkFields(response, ['fromUtc', 'toUtc', 'previousFromUtc', 'current', 'previous', 'byDay', 'eventTotal']);
    return {
        ...value,
        current: marketingAttributionSummaryFromSdk(value.current),
        previous: marketingAttributionSummaryFromSdk(value.previous),
        byDay: value.byDay.map(item => marketingAttributionDayFromSdk(item)),
    };
}

export function marketingAttributionDayFromSdk(response: MarketingAttributionDayHttpResponse): {
    date: string;
    visits: number;
    signups: number;
    premiumStarts: number;
} {
    const value = requireSdkFields(response, ['date', 'visits', 'signups', 'premiumStarts']);
    return { ...value };
}
