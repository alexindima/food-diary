export type MarketingAttributionBreakdown = {
    source: string;
    medium: string;
    campaign: string;
    events: number;
    visits: number;
    signups: number;
    premiumStarts: number;
    anonymousVisitors: number;
    sessions: number;
    signupRatePercent: number;
    premiumRatePercent: number;
    lastEventAtUtc: string | null;
};

export type MarketingAttributionRecentEvent = {
    occurredAtUtc: string;
    eventType: string;
    anonymousId: string;
    sessionId: string;
    landingPath: string;
    referrerHost: string | null;
    utmSource: string | null;
    utmMedium: string | null;
    utmCampaign: string | null;
    utmContent: string | null;
    utmTerm: string | null;
    buildVersion: string | null;
};

export type MarketingAttributionSummary = {
    windowHours: number;
    generatedAtUtc: string;
    events: number;
    visits: number;
    signups: number;
    premiumStarts: number;
    anonymousVisitors: number;
    sessions: number;
    attributedEvents: number;
    organicEvents: number;
    signupRatePercent: number;
    premiumRatePercent: number;
    lastEventAtUtc: string | null;
    topCampaigns: MarketingAttributionBreakdown[];
    topSources: MarketingAttributionBreakdown[];
    recentEvents: MarketingAttributionRecentEvent[];
};
