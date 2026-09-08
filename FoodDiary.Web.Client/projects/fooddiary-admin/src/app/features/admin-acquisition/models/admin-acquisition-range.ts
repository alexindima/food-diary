import type { MarketingAttributionSummary } from './admin-acquisition.data';

export type MarketingAttributionRange = {
    fromUtc: string;
    toUtc: string;
    previousFromUtc: string;
    current: MarketingAttributionSummary;
    previous: MarketingAttributionSummary;
    byDay: Array<{ date: string; visits: number; signups: number; premiumStarts: number }>;
    eventTotal: number;
};
